using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Account;

public class ForgotPasswordAccountViewModel
{
    [Required(ErrorMessage = "Skriv din e-mail.")]
    [EmailAddress(ErrorMessage = "Skriv en gyldig e-mailadresse.")]
    [StringLength(100, ErrorMessage = "E-mailen må højst være {1} tegn.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
}
