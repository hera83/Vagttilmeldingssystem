using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Account;

/// <summary>Fælles base for SetPassword (invitation) og ResetPassword (glemt adgangskode), så de kan dele _PasswordLinkFormPartial.</summary>
public abstract class PasswordLinkFormViewModel
{
    /// <summary>Engangskoden fra linket. Sendes med i formularen som skjult felt.</summary>
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv en ny adgangskode.")]
    [StringLength(200, MinimumLength = 12, ErrorMessage = "Adgangskoden skal være mindst {2} tegn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Ny adgangskode")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "De to adgangskoder er ikke ens.")]
    [DataType(DataType.Password)]
    [Display(Name = "Gentag ny adgangskode")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>Kun visning: hvorfor linket ikke kan bruges, eller null, hvis det er gyldigt.</summary>
    public string? LinkProblem { get; set; }

    /// <summary>Kun visning: formularens handling (SetPassword eller ResetPassword).</summary>
    public abstract string FormAction { get; }
}
