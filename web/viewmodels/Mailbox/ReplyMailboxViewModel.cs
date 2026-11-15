using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Mailbox;

public class ReplyMailboxViewModel
{
    public string Folder { get; set; } = "inbox";

    /// <summary>Kun visning: modtageren (fra den oprindelige mail; kan ikke ændres).</summary>
    public string ToText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv et emne.")]
    [StringLength(300, ErrorMessage = "Emnet må højst være {1} tegn.")]
    [Display(Name = "Emne")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv en besked.")]
    [StringLength(20000, ErrorMessage = "Beskeden må højst være {1} tegn.")]
    [Display(Name = "Besked")]
    public string Body { get; set; } = string.Empty;
}
