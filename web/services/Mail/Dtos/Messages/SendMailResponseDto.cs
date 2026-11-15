namespace web.Services.Mail.Dtos.Messages;

public class SendMailResponseDto
{
    public string MessageId { get; set; } = string.Empty;
    public MailDelivery Delivery { get; set; }

    /// <summary>Kun med SaveCopyToSent: fejlen, hvis kopien ikke kunne gemmes i Sendt (mailen er sendt alligevel).</summary>
    public string? SentCopyError { get; set; }
}
