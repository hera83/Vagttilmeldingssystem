using web.Managers.Mailbox.Dtos;
using web.Services.Mail.Dtos.Mailbox;

namespace web.Managers.Mailbox.Interfaces;

public interface IMailboxManager
{
    /// <summary>Én side af en mappe med genkendte brugere. IMAP-fejl kommer tilbage i Error (siden vælter ikke).</summary>
    Task<MailboxPageDto> GetPageAsync(MailFolder folder, string? search, int page, CancellationToken cancellationToken = default);

    /// <summary>Mailen med renset HTML og genkendt afsender. Markerer den som læst. null, hvis den ikke findes.</summary>
    Task<MailboxMessageDto?> ReadAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default);

    Task<ReplyDraftDto?> GetReplyDraftAsync(MailFolder folder, uint uid, string signatureName, CancellationToken cancellationToken = default);

    Task<SendMailboxResultDto> SendReplyAsync(ReplyDto dto, CancellationToken cancellationToken = default);

    Task<SendMailboxResultDto> SendNewAsync(ComposeDto dto, CancellationToken cancellationToken = default);

    Task<List<RecipientDto>> GetRecipientsAsync(CancellationToken cancellationToken = default);

    Task<bool> MarkUnreadAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default);

    /// <summary>Flytter til Papirkurv, eller sletter helt, hvis mailen ligger i Papirkurv.</summary>
    Task<bool> DeleteAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default);

    Task<MailAttachmentDto?> GetAttachmentAsync(MailFolder folder, uint uid, int index, CancellationToken cancellationToken = default);
}
