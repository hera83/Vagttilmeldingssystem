using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Users;

public class ResetPasswordUsersViewModel
{
    /// <summary>Kun til visning.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Kun til visning.</summary>
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv en ny adgangskode.")]
    [StringLength(200, MinimumLength = 12, ErrorMessage = "Adgangskoden skal være mindst {2} tegn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Ny adgangskode")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "De to adgangskoder er ikke ens.")]
    [DataType(DataType.Password)]
    [Display(Name = "Gentag ny adgangskode")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
