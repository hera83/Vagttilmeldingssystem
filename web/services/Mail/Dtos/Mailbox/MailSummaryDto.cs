namespace web.Services.Mail.Dtos.Mailbox;

/// <summary>Én mail i listen (uden indhold).</summary>
public class MailSummaryDto
{
    public uint Uid { get; set; }
    public MailAddressDto From { get; set; } = new();
    public List<MailAddressDto> To { get; set; } = [];
    public string Subject { get; set; } = string.Empty;
    public DateTime DateUtc { get; set; }
    public bool Seen { get; set; }
    public bool HasAttachments { get; set; }
}
