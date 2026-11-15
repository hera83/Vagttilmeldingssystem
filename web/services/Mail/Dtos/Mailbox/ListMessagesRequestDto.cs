namespace web.Services.Mail.Dtos.Mailbox;

public class ListMessagesRequestDto
{
    public MailFolder Folder { get; set; } = MailFolder.Inbox;

    /// <summary>Søger i afsender og emne.</summary>
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
