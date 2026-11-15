using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Users;

public class CreateUsersViewModel
{
    [Required(ErrorMessage = "Skriv brugerens e-mail.")]
    [EmailAddress(ErrorMessage = "Skriv en gyldig e-mailadresse.")]
    [StringLength(100, ErrorMessage = "E-mailen må højst være {1} tegn.")]
    [Display(Name = "E-mail (brugernavn)")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv brugerens navn.")]
    [StringLength(200, ErrorMessage = "Navnet må højst være {1} tegn.")]
    [Display(Name = "Navn")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Standard til i formularen (sættes af GET). Så vælger brugeren selv sin adgangskode via et link i en mail.</summary>
    [Display(Name = "Send invitation på e-mail")]
    public bool SendInvitation { get; set; }

    /// <summary>Kun når der ikke sendes invitation. Controlleren fjerner kravet, når SendInvitation er valgt.</summary>
    [Required(ErrorMessage = "Skriv en adgangskode, eller vælg Send invitation på e-mail.")]
    [StringLength(200, MinimumLength = 12, ErrorMessage = "Adgangskoden skal være mindst {2} tegn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Adgangskode")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Administrator")]
    public bool IsAdmin { get; set; }
}
