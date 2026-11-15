using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Mailbox;

public class ComposeMailboxViewModel
{
    [Required(ErrorMessage = "Skriv modtagerens e-mail.")]
    [EmailAddress(ErrorMessage = "Skriv en gyldig e-mailadresse.")]
    [StringLength(200, ErrorMessage = "Adressen må højst være {1} tegn.")]
    [Display(Name = "Til")]
    public string ToAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv et emne.")]
    [StringLength(300, ErrorMessage = "Emnet må højst være {1} tegn.")]
    [Display(Name = "Emne")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv en besked.")]
    [StringLength(20000, ErrorMessage = "Beskeden må højst være {1} tegn.")]
    [Display(Name = "Besked")]
    public string Body { get; set; } = string.Empty;

    /// <summary>Kun visning: brugere til forslagslisten (datalist). Fyldes igen, når formularen vises igen.</summary>
    public List<RecipientOptionViewModel> Recipients { get; set; } = [];
}
