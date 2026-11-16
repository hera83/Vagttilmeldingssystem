using web.Managers.Members.Dtos;
using web.Managers.Members.Interfaces;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.PasswordTokens.Interfaces;
using web.Repositories.PushSubscriptions.Interfaces;
using web.Services.Push;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Users.Interfaces;

namespace web.Managers.Members;

public class MemberManager(
    IUserRepository userRepository,
    IProfileRepository profileRepository,
    IAssignmentRepository assignmentRepository,
    IPushSubscriptionRepository pushSubscriptionRepository,
    INotificationRepository notificationRepository,
    IPasswordTokenRepository passwordTokenRepository,
    TimeProvider timeProvider) : IMemberManager
{
    public const int ExportNotificationLimit = 500;

    public async Task<AnonymizeMemberResult> AnonymizeMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            return AnonymizeMemberResult.NotFound;
        }

        // Brugeren anonymiseres til sidst: fejler et trin, kan handlingen køres igen og gøre resten færdigt.
        await profileRepository.DeleteAsync(userId, cancellationToken);
        await pushSubscriptionRepository.DeleteAllForUserAsync(userId, cancellationToken);
        await notificationRepository.DeletePendingForUserAsync(userId, cancellationToken);
        await passwordTokenRepository.DeleteUnusedForUserAsync(userId, cancellationToken);
        await userRepository.AnonymizeAsync(userId, cancellationToken);
        return AnonymizeMemberResult.Anonymized;
    }

    public async Task<MemberDataExportDto?> ExportMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return new MemberDataExportDto
        {
            ExportedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
            User = user,
            Profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken),
            EmailNotifications = await profileRepository.GetEmailNotificationsAsync(userId, cancellationToken),
            Assignments = await assignmentRepository.GetHistoryForUserAsync(userId, cancellationToken),
            Devices = (await pushSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken)).Select(x => new MemberDeviceDto
            {
                PushService = PushEndpoints.HostOf(x.Endpoint),
                CreatedAt = x.CreatedAt,
                LastSuccessAt = x.LastSuccessAt
            }).ToList(),
            Notifications = await notificationRepository.GetForUserAsync(userId, ExportNotificationLimit, cancellationToken)
        };
    }

    public async Task<DeleteMemberResult> DeleteMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await assignmentRepository.AnyForUserAsync(userId, cancellationToken))
        {
            return DeleteMemberResult.HasAssignments;
        }

        return await userRepository.DeleteAsync(userId, cancellationToken) ? DeleteMemberResult.Deleted : DeleteMemberResult.NotFound;
    }

    public async Task<List<MemberDto>> GetMembersAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var complete = await profileRepository.GetCompleteUserIdsAsync(cancellationToken);
        return users.Select(user => new MemberDto { User = user, HasCompleteProfile = complete.Contains(user.Id) }).ToList();
    }

    public async Task<MemberDetailsDto?> GetMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return new MemberDetailsDto
        {
            User = user,
            Profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken)
        };
    }
}
