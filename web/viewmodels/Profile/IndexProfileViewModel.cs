using System.ComponentModel.DataAnnotations;
using web.Repositories.Profiles.Dtos;

namespace web.ViewModels.Profile;

public class IndexProfileViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Skriv dit fulde navn.")]
    [StringLength(200, ErrorMessage = "Navnet må højst være {1} tegn.")]
    [Display(Name = "Fulde navn")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv din adresse.")]
    [StringLength(300, ErrorMessage = "Adressen må højst være {1} tegn.")]
    [Display(Name = "Adresse")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv dit telefonnummer.")]
    [StringLength(30, ErrorMessage = "Telefonnummeret må højst være {1} tegn.")]
    [RegularExpression(@"^\+?[0-9][0-9 ]{6,18}[0-9]$", ErrorMessage = "Skriv et gyldigt telefonnummer, fx 12 34 56 78 eller +45 12345678.")]
    [Display(Name = "Telefonnummer")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vælg en trøjestørrelse.")]
    [Display(Name = "Trøjestørrelse")]
    public string ShirtSize { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv antal væskebilletter (0, hvis du ikke ønsker nogen).")]
    [Range(0, 50, ErrorMessage = "Væskebilletter skal være mellem {1} og {2}.")]
    [Display(Name = "Væskebilletter")]
    public int? DrinkTickets { get; set; }

    [Required(ErrorMessage = "Vælg antal ark tivolibilletter (0, hvis du ikke ønsker nogen).")]
    [Range(0, 2, ErrorMessage = "Du kan højst få 2 ark tivolibilletter.")]
    [Display(Name = "Tivolibilletter (ark)")]
    public int? TivoliSheets { get; set; }

    [Required(ErrorMessage = "Skriv barnets navn.")]
    [StringLength(200, ErrorMessage = "Barnets navn må højst være {1} tegn.")]
    [Display(Name = "Barnets navn")]
    public string ChildName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv barnets klasse.")]
    [StringLength(20, ErrorMessage = "Klassen må højst være {1} tegn.")]
    [Display(Name = "Klasse")]
    public string ChildClass { get; set; } = string.Empty;

    /// <summary>Kun til visning: brugerens e-mail (brugernavnet). Kan kun ændres af en administrator.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Kun til visning.</summary>
    public bool IsComplete { get; set; }

    /// <summary>Kun visning: e-mail-beskeder slået til (gemmes via NotificationsProfileViewModel).</summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>Kun til visning: valgmulighederne for trøjestørrelse.</summary>
    public IReadOnlyList<string> ShirtSizeOptions { get; set; } = ShirtSizes.All;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(ShirtSize) && !ShirtSizes.IsValid(ShirtSize))
        {
            yield return new ValidationResult("Vælg en trøjestørrelse fra listen.", [nameof(ShirtSize)]);
        }
    }
}
