namespace web.ViewModels.Mailbox;

public class IndexMailboxViewModel
{
    /// <summary>"inbox", "sent" eller "trash" (bruges i links).</summary>
    public string Folder { get; set; } = "inbox";

    public string FolderTitle { get; set; } = string.Empty;

    /// <summary>"Fra" i Indbakke og Papirkurv, "Til" i Sendt.</summary>
    public string PersonColumnTitle { get; set; } = "Fra";

    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public string? Error { get; set; }
    public List<MailListItemViewModel> Items { get; set; } = [];
}
