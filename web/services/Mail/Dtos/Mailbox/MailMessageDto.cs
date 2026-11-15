namespace web.Services.Mail.Dtos.Mailbox;

/// <summary>Én mail med indhold. HtmlBody er rå HTML fra afsenderen og skal renses, før den vises.</summary>
public class MailMessageDto
{
    public uint Uid { get; set; }
    public MailFolder Folder { get; set; }
    public MailAddressDto From { get; set; } = new();

    /// <summary>Reply-To, hvis afsenderen har sat den, ellers null.</summary>
    public MailAddressDto? ReplyTo { get; set; }

    public List<MailAddressDto> To { get; set; } = [];
    public List<MailAddressDto> Cc { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public DateTime DateUtc { get; set; }
    public string? MessageId { get; set; }
    public List<string> References { get; set; } = [];
    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
    public List<MailAttachmentInfoDto> Attachments { get; set; } = [];
    public bool Seen { get; set; }
}
