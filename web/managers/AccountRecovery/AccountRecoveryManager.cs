using web.Data.Models;
using web.Infrastructure.Authentication;
using web.Infrastructure.Notifications;
using web.Managers.AccountRecovery.Dtos;
using web.Managers.AccountRecovery.Interfaces;
using web.Repositories.Notifications.Dtos;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.PasswordTokens.Dtos;
using web.Repositories.PasswordTokens.Interfaces;
using web.Repositories.Users.Dtos;
using web.Repositories.Users.Interfaces;
using web.Services.Mail.Interfaces;

namespace web.Managers.AccountRecovery;

/// <summary>Invitation af nye brugere og glemt adgangskode via engangslinks i mails (sendes via køen).</summary>
public class AccountRecoveryManager(
    IUserRepository userRepository,
    IPasswordTokenRepository passwordTokenRepository,
    INotificationRepository notificationRepository,
    IMailService mailService,
    AppLinks appLinks,
    NotificationSignal signal,
    TimeProvider timeProvider,
    ILogger<AccountRecoveryManager> logger) : IAccountRecoveryManager
{
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);

    public async Task<InviteUserResultDto> InviteNewUserAsync(InviteUserDto dto, CancellationToken cancellationToken = default)
    {
        if (!await CanSendLinksAsync(cancellationToken))
        {
            return new InviteUserResultDto { Status = InviteUserStatus.MailNotConfigured };
        }

        // Adgangskoden kender ingen: brugeren vælger sin egen via linket, så administratoren aldrig kender den.
        var user = await userRepository.CreateAsync(new CreateUserDto
        {
            UserName = dto.UserName,
            DisplayName = dto.DisplayName,
            Password = PasswordTokenGenerator.GenerateUnknownPassword(),
            IsAdmin = dto.IsAdmin
        }, cancellationToken);
        if (user is null)
        {
            return new InviteUserResultDto { Status = InviteUserStatus.UserNameTaken };
        }

        await QueueInvitationAsync(user, cancellationToken);
        return new InviteUserResultDto { Status = InviteUserStatus.Success, User = user };
    }

    public async Task<SendInvitationResult> SendInvitationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return SendInvitationResult.NotFound;
        }

        if (!user.IsActive)
        {
            return SendInvitationResult.Inactive;
        }

        if (!await CanSendLinksAsync(cancellationToken))
        {
            return SendInvitationResult.MailNotConfigured;
        }

        await QueueInvitationAsync(user, cancellationToken);
        return SendInvitationResult.Sent;
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!await CanSendLinksAsync(cancellationToken))
        {
            // Samme svar til brugeren uanset hvad. Driften kan se det i loggen.
            logger.LogWarning("Password reset requested, but mail or App:BaseUrl is not configured");
            return;
        }

        var user = await userRepository.GetByUserNameAsync(email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var issued = await passwordTokenRepository.CreateAsync(new CreatePasswordTokenDto
        {
            UserId = user.Id,
            Purpose = PasswordTokenPurpose.Reset,
            ExpiresAt = now + ResetLifetime
        }, cancellationToken);

        var link = appLinks.Absolute(ResetPath(issued.Token));
        var mail = MailTemplates.PasswordReset(link, (int)ResetLifetime.TotalMinutes);
        await QueueAsync(user.Id, NotificationKinds.PasswordReset, mail, link, $"reset:{issued.PasswordToken.Id:N}", now, cancellationToken);
    }

    public async Task<PasswordLinkStatus> CheckLinkAsync(string token, PasswordTokenPurpose purpose, CancellationToken cancellationToken = default) =>
        Status(await FindAsync(token, cancellationToken), purpose);

    public async Task<PasswordLinkStatus> SetPasswordAsync(string token, PasswordTokenPurpose purpose, string newPassword, CancellationToken cancellationToken = default)
    {
        var link = await FindAsync(token, cancellationToken);
        var status = Status(link, purpose);
        if (status != PasswordLinkStatus.Valid)
        {
            return status;
        }

        // Atomart: kun det første forsøg bruger linket (to faner, dobbeltklik).
        if (!await passwordTokenRepository.ConsumeAsync(link!.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken))
        {
            return PasswordLinkStatus.Used;
        }

        if (!await userRepository.SetPasswordAsync(link.UserId, newPassword, cancellationToken))
        {
            return PasswordLinkStatus.Invalid;
        }

        await passwordTokenRepository.DeleteUnusedForUserAsync(link.UserId, cancellationToken);
        return PasswordLinkStatus.Valid;
    }

    private async Task QueueInvitationAsync(UserDto user, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var issued = await passwordTokenRepository.CreateAsync(new CreatePasswordTokenDto
        {
            UserId = user.Id,
            Purpose = PasswordTokenPurpose.Invitation,
            ExpiresAt = now + InvitationLifetime
        }, cancellationToken);

        var link = appLinks.Absolute(InvitationPath(issued.Token));
        var mail = MailTemplates.Invitation(user.UserName, link, (int)InvitationLifetime.TotalDays);
        await QueueAsync(user.Id, NotificationKinds.Invitation, mail, link, $"invitation:{issued.PasswordToken.Id:N}", now, cancellationToken);
    }

    private async Task QueueAsync(Guid userId, string kind, MailText mail, string url, string dedupKey, DateTime now, CancellationToken cancellationToken)
    {
        await notificationRepository.CreateManyAsync(
        [
            new CreateNotificationDto
            {
                UserId = userId,
                Channel = NotificationChannel.Email,
                Kind = kind,
                Title = mail.Subject,
                Body = mail.Body,
                Url = url,
                DedupKey = dedupKey,
                NotBeforeUtc = now
            }
        ], cancellationToken);
        signal.Notify();
    }

    private async Task<PasswordTokenDto?> FindAsync(string token, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(token) || token.Length > 100 ? null : await passwordTokenRepository.FindAsync(token, cancellationToken);

    private PasswordLinkStatus Status(PasswordTokenDto? link, PasswordTokenPurpose purpose)
    {
        if (link is null || link.Purpose != purpose)
        {
            return PasswordLinkStatus.Invalid;
        }

        if (link.UsedAt is not null)
        {
            return PasswordLinkStatus.Used;
        }

        return link.ExpiresAt <= timeProvider.GetUtcNow().UtcDateTime ? PasswordLinkStatus.Expired : PasswordLinkStatus.Valid;
    }

    private async Task<bool> CanSendLinksAsync(CancellationToken cancellationToken) =>
        appLinks.IsConfigured && await mailService.IsConfiguredAsync(cancellationToken);

    private static string InvitationPath(string token) => $"/Account/SetPassword?token={Uri.EscapeDataString(token)}";

    private static string ResetPath(string token) => $"/Account/ResetPassword?token={Uri.EscapeDataString(token)}";
}
