namespace web.Services.Mail.Dtos.Mailbox;

public class MailAttachmentInfoDto
{
    /// <summary>Vedhæftningens nummer i mailen (0, 1, …). Bruges til at hente den.</summary>
    public int Index { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
