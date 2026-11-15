using web.Services.Mail.Dtos.Mailbox;

namespace web.Managers.Mailbox.Dtos;

public class MailboxMessageDto
{
    public MailMessageDto Message { get; set; } = new();

    /// <summary>HTML-udgaven renset med MailHtmlSanitizer (ingen scripts, events, formularer eller billeder), eller null.</summary>
    public string? SafeHtml { get; set; }

    /// <summary>Tekstudgaven. Er der kun HTML, er det teksten fra HTML'en.</summary>
    public string Text { get; set; } = string.Empty;

    public KnownUserDto? Sender { get; set; }
}
