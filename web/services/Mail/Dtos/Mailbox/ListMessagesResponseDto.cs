namespace web.Services.Mail.Dtos.Mailbox;

public class ListMessagesResponseDto
{
    public List<MailSummaryDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }

    /// <summary>Antal ulæste i mappen.</summary>
    public int UnreadCount { get; set; }
}
