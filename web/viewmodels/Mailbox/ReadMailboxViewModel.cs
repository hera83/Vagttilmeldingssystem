namespace web.ViewModels.Mailbox;

public class ReadMailboxViewModel
{
    public string Folder { get; set; } = "inbox";
    public string FolderTitle { get; set; } = string.Empty;
    public uint Uid { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FromText { get; set; } = string.Empty;
    public Guid? KnownUserId { get; set; }
    public string? KnownUserName { get; set; }
    public string ToText { get; set; } = string.Empty;
    public string CcText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    /// <summary>Renset HTML (MailHtmlSanitizer). Vises kun, når brugeren beder om det, eller når der ikke er tekst.</summary>
    public string? SafeHtml { get; set; }

    public bool ShowHtml { get; set; }
    public bool IsTrash { get; set; }
    public bool IsSent { get; set; }
    public List<MailAttachmentViewModel> Attachments { get; set; } = [];
}
