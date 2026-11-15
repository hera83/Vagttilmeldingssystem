namespace web.Services.Mail.Dtos.Messages;

public class SendMailResponseDto
{
    public string MessageId { get; set; } = string.Empty;
    public MailDelivery Delivery { get; set; }
}
