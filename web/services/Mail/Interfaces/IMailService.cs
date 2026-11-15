using web.Services.Mail.Dtos.Mailbox;
using web.Services.Mail.Dtos.Messages;

namespace web.Services.Mail.Interfaces;

public interface IMailService
{
    /// <summary>Kan der sendes mails? Mail:FromAddress og enten Mail:Smtp:Host eller Mail:PickupDirectory skal være sat.</summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>Sender én mail fra systemets postkasse (Mail:FromAddress).</summary>
    Task<SendMailResponseDto> SendAsync(SendMailRequestDto request, CancellationToken cancellationToken = default);

    // Systemets postkasse via IMAP (webmailen, step 18).

    /// <summary>Er IMAP sat op (Mail:Imap:Host)?</summary>
    Task<bool> IsMailboxConfiguredAsync(CancellationToken cancellationToken = default);

    /// <summary>Antal ulæste i Indbakke.</summary>
    Task<MailboxStatusDto> GetMailboxStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Mails i en mappe, nyeste først, med søgning i afsender og emne og sider.</summary>
    Task<ListMessagesResponseDto> ListMessagesAsync(ListMessagesRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Én mail med indhold, eller null. Markerer den ikke som læst.</summary>
    Task<MailMessageDto?> GetMessageAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default);

    /// <summary>Markér som læst eller ulæst. false, hvis mailen ikke findes.</summary>
    Task<bool> SetSeenAsync(MailFolder folder, uint uid, bool seen, CancellationToken cancellationToken = default);

    /// <summary>Flytter mailen til Papirkurv, eller sletter den helt, hvis den allerede ligger der. false, hvis den ikke findes.</summary>
    Task<bool> DeleteAsync(MailFolder folder, uint uid, CancellationToken cancellationToken = default);

    /// <summary>Én vedhæftning, eller null.</summary>
    Task<MailAttachmentDto?> GetAttachmentAsync(MailFolder folder, uint uid, int index, CancellationToken cancellationToken = default);
}
