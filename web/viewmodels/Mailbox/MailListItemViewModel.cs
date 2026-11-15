namespace web.ViewModels.Mailbox;

public class MailListItemViewModel
{
    public uint Uid { get; set; }

    /// <summary>Afsender (Indbakke/Papirkurv) eller modtager (Sendt): navn eller adresse.</summary>
    public string PersonText { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public Guid? KnownUserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public bool Seen { get; set; }
    public bool HasAttachments { get; set; }
}
