using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Users;

public class EditUsersViewModel
{
    /// <summary>Kun til visning. Sendes ikke videre til repositoriet.</summary>
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv brugerens navn.")]
    [StringLength(200, ErrorMessage = "Navnet må højst være {1} tegn.")]
    [Display(Name = "Navn")]
    public string DisplayName { get; set; } = string.Empty;

    [Display(Name = "Administrator")]
    public bool IsAdmin { get; set; }

    [Display(Name = "Aktiv (kan logge ind)")]
    public bool IsActive { get; set; }

    /// <summary>Kun til visning: er det den indloggede administrators egen konto?</summary>
    public bool IsCurrentUser { get; set; }
}
