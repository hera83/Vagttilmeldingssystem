using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Events;

public class EditEventsViewModel : IValidatableObject
{
    /// <summary>Kun til visning (links). Id'et kommer fra routen.</summary>
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Skriv et navn på begivenheden.")]
    [StringLength(200, ErrorMessage = "Navnet må højst være {1} tegn.")]
    [Display(Name = "Navn")]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Beskrivelsen må højst være {1} tegn.")]
    [Display(Name = "Beskrivelse")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Skriv, hvor begivenheden foregår.")]
    [StringLength(200, ErrorMessage = "Stedet må højst være {1} tegn.")]
    [Display(Name = "Sted")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vælg en startdato.")]
    [Display(Name = "Startdato")]
    public DateOnly? StartDate { get; set; }

    [Required(ErrorMessage = "Vælg en slutdato.")]
    [Display(Name = "Slutdato")]
    public DateOnly? EndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        EventsViewModelMapper.ValidatePeriod(StartDate, EndDate);
}
