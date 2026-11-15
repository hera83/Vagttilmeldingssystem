using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Account;

public class LoginAccountViewModel
{
    [Required(ErrorMessage = "Skriv din e-mail.")]
    [Display(Name = "E-mail")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv din adgangskode.")]
    [DataType(DataType.Password)]
    [Display(Name = "Adgangskode")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Husk mig")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
