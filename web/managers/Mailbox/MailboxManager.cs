using System.Text;
using AngleSharp.Html.Parser;
using web.Extensions;
using web.Infrastructure.Mail;
using web.Infrastructure.Notifications;
using web.Managers.Mailbox.Dtos;
using web.Managers.Mailbox.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Services.Mail;
using web.Services.Mail.Dtos.Mailbox;
using web.Services.Mail.Dtos.Messages;
using web.Services.Mail.Interfaces;

namespace web.Managers.Mailbox;

/// <summary>
/// Webmailen til systemets postkasse (krav K22). Mails læses direkte fra IMAP og gemmes aldrig i app.db.
/// Afsendere og modtagere genkendes som brugere ud fra e-mailen.
/// </summary>
public class MailboxManager(
    IMailService mailService,
    IUserRepository userRepository,
    MailHtmlSanitizer sanitizer,
    MailboxStatus mailboxStatus,
    TimeProvider timeProvider) : IMailboxManager
{
    public const int PageSize = 50;

    public async Task<MailboxPageDto> GetPageAsync(MailFolder folder, string? search, int page, CancellationToken cancellationToken = default)
    {
        var result = new MailboxPageDto { Folder = folder, Search = search, Page = Math.Max(1, page), PageSize = PageSize };
        if (!await mailService.IsMailboxConfiguredAsync(cancellationToken))
        {
            mailboxStatus.Configured(false);
            result.Error = "Webmailen er ikke sat op. Administratoren af serveren skal sætte Mail:Imap:Host, Mail:Imap:UserName og Mail:Imap:Password.";
            return result;
        }

        try
        {
            var list = await mailService.ListMessagesAsync(new ListMessagesRequestDto
            {
                Folder = folder,
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Page = result.Page,
                PageSize = PageSize
            }, cancellationToken);

            var users = await UsersByEmailAsync(cancellationToken);
            result.TotalCount = list.TotalCount;
            result.UnreadCount = list.UnreadCount;
            result.Items = list.Items.Select(x => new MailboxItemDto
            {
                Summary = x,
                KnownUser = Known(users, folder == MailFolder.Sent ? x.To.FirstOrDefault()?.Address : x.From.Address)
            }).ToList();

            if (folder == MailFolder.Inbox && string.IsNullOrWhiteSpace(search))
            {
                mailboxStatus.Succeeded(list.UnreadCount, timeProvider.GetUtcNow().UtcDateTime);
            }
        }
        catch (MailException ex)
        {
            // Forventet udfald for siden: postkassen kan ikke nås lige nu (bevidst afvigelse, se projektfilen).
            mailboxStatus.Failed(ex.Message, timeProvider.GetUtcNow().UtcDateTime);
            result.Error = $"Postkassen kunne ikke nås: {ex.Message}";
        }

        return result;
    }

    public async Task<MailboxMessageDto?> ReadAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default)
    {
        var message = await mailService.GetMessageAsync(folder, uid, cancellationToken);
        if (message is null)
        {
            return null;
        }

        if (!message.Seen)
        {
            await mailService.SetSeenAsync(folder, uid, true, cancellationToken);
            message.Seen = true;
        }

        var users = await UsersByEmailAsync(cancellationToken);
        return new MailboxMessageDto
        {
            Message = message,
            SafeHtml = string.IsNullOrWhiteSpace(message.HtmlBody) ? null : sanitizer.Sanitize(message.HtmlBody),
            Text = TextOf(message),
            Sender = Known(users, folder == MailFolder.Sent ? message.To.FirstOrDefault()?.Address : message.From.Address)
        };
    }

    public async Task<ReplyDraftDto?> GetReplyDraftAsync(MailFolder folder, uint uid, string signatureName, CancellationToken cancellationToken = default)
    {
        var message = await mailService.GetMessageAsync(folder, uid, cancellationToken);
        if (message is null)
        {
            return null;
        }

        var to = RecipientOf(message);
        var quote = new StringBuilder();
        quote.Append($"Den {message.DateUtc.ToDanishDateTimeText()} skrev {(string.IsNullOrWhiteSpace(message.From.Name) ? message.From.Address : $"{message.From.Name} <{message.From.Address}>")}:\n");
        foreach (var line in TextOf(message).Replace("\r\n", "\n").Split('\n'))
        {
            quote.Append("> ").Append(line).Append('\n');
        }

        return new ReplyDraftDto
        {
            Folder = folder,
            Uid = uid,
            To = to,
            KnownUser = Known(await UsersByEmailAsync(cancellationToken), to.Address),
            Subject = ReplySubject(message.Subject),
            Body = $"\n\nVenlig hilsen\n{signatureName}\n\n{quote.ToString().TrimEnd()}"
        };
    }

    public async Task<SendMailboxResultDto> SendReplyAsync(ReplyDto dto, CancellationToken cancellationToken = default)
    {
        // Modtager og tråd tages fra den oprindelige mail, så formularen ikke kan sende til en anden adresse.
        var original = await mailService.GetMessageAsync(dto.Folder, dto.Uid, cancellationToken);
        if (original is null)
        {
            return new SendMailboxResultDto { Status = SendMailboxStatus.NotFound };
        }

        var to = RecipientOf(original);
        return await SendAsync(new SendMailRequestDto
        {
            ToAddress = to.Address,
            ToName = to.Name,
            Subject = dto.Subject.Trim(),
            TextBody = dto.Body,
            HtmlBody = MailTemplates.ToHtml(dto.Subject.Trim(), dto.Body, null, string.Empty),
            InReplyTo = original.MessageId,
            References = original.References,
            SaveCopyToSent = true
        }, cancellationToken);
    }

    public Task<SendMailboxResultDto> SendNewAsync(ComposeDto dto, CancellationToken cancellationToken = default) =>
        SendAsync(new SendMailRequestDto
        {
            ToAddress = dto.ToAddress.Trim(),
            Subject = dto.Subject.Trim(),
            TextBody = dto.Body,
            HtmlBody = MailTemplates.ToHtml(dto.Subject.Trim(), dto.Body, null, string.Empty),
            SaveCopyToSent = true
        }, cancellationToken);

    public async Task<List<RecipientDto>> GetRecipientsAsync(CancellationToken cancellationToken = default) =>
        (await userRepository.GetAllAsync(cancellationToken))
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayName)
            .Select(x => new RecipientDto { DisplayName = x.DisplayName, Email = x.UserName })
            .ToList();

    public Task<bool> MarkUnreadAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default) =>
        mailService.SetSeenAsync(folder, uid, false, cancellationToken);

    public Task<bool> DeleteAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default) =>
        mailService.DeleteAsync(folder, uid, cancellationToken);

    public Task<MailAttachmentDto?> GetAttachmentAsync(MailFolder folder, uint uid, int index, CancellationToken cancellationToken = default) =>
        mailService.GetAttachmentAsync(folder, uid, index, cancellationToken);

    private async Task<SendMailboxResultDto> SendAsync(SendMailRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await mailService.SendAsync(request, cancellationToken);
            return new SendMailboxResultDto { Status = SendMailboxStatus.Sent, SentCopyError = response.SentCopyError };
        }
        catch (MailException ex)
        {
            // Formularen vises igen med fejlen, så teksten ikke går tabt (bevidst afvigelse, se projektfilen).
            return new SendMailboxResultDto
            {
                Status = ex.StatusCode == System.Net.HttpStatusCode.BadRequest ? SendMailboxStatus.InvalidRecipient : SendMailboxStatus.Failed,
                Error = ex.Message
            };
        }
    }

    private async Task<Dictionary<string, KnownUserDto>> UsersByEmailAsync(CancellationToken cancellationToken) =>
        (await userRepository.GetAllAsync(cancellationToken))
            .GroupBy(x => x.UserName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => new KnownUserDto { UserId = g.First().Id, DisplayName = g.First().DisplayName }, StringComparer.OrdinalIgnoreCase);

    private static KnownUserDto? Known(Dictionary<string, KnownUserDto> users, string? email) =>
        email is not null && users.TryGetValue(email, out var user) ? user : null;

    private static MailAddressDto RecipientOf(MailMessageDto message) =>
        message.ReplyTo is { Address.Length: > 0 } replyTo ? replyTo : message.From;

    private static string ReplySubject(string subject) =>
        subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) || subject.StartsWith("Sv:", StringComparison.OrdinalIgnoreCase)
            ? subject
            : $"Re: {subject}";

    private string TextOf(MailMessageDto message)
    {
        if (!string.IsNullOrWhiteSpace(message.TextBody))
        {
            return message.TextBody.Trim();
        }

        if (string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            return string.Empty;
        }

        // Renses først, så scripts og styles ikke kommer med i teksten.
        var document = new HtmlParser().ParseDocument(sanitizer.Sanitize(message.HtmlBody));
        return (document.Body?.TextContent ?? string.Empty).Trim();
    }
}
