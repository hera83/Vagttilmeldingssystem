using web.Services.Mail.Dtos.Mailbox;

namespace web.Managers.Mailbox.Dtos;

public class MailboxPageDto
{
    public MailFolder Folder { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public List<MailboxItemDto> Items { get; set; } = [];

    /// <summary>Forståelig fejl, hvis postkassen ikke kunne nås eller ikke er sat op. Siden vises alligevel.</summary>
    public string? Error { get; set; }
}
