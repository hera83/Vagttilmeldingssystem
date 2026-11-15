using System.ComponentModel.DataAnnotations;

namespace web.ViewModels.Shifts;

/// <summary>Fælles felter og validering for Opret vagt og Ret vagt (views/Shared/_ShiftFieldsPartial.cshtml).</summary>
public abstract class ShiftFormViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Skriv et navn på vagten.")]
    [StringLength(200, ErrorMessage = "Navnet må højst være {1} tegn.")]
    [Display(Name = "Vagt-navn")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vælg en dato.")]
    [Display(Name = "Dato")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Vælg et starttidspunkt.")]
    [Display(Name = "Starttid")]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Vælg et sluttidspunkt.")]
    [Display(Name = "Sluttid")]
    public TimeOnly? EndTime { get; set; }

    [Required(ErrorMessage = "Skriv antal pladser.")]
    [Range(1, 500, ErrorMessage = "Antal pladser skal være mellem {1} og {2}.")]
    [Display(Name = "Antal pladser")]
    public int? Capacity { get; set; }

    [StringLength(1000, ErrorMessage = "Beskrivelsen må højst være {1} tegn.")]
    [Display(Name = "Beskrivelse")]
    public string? Description { get; set; }

    /// <summary>Kun til visning.</summary>
    public Guid EventId { get; set; }

    /// <summary>Kun til visning.</summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>Kun til visning.</summary>
    public string EventPeriodText { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        ShiftsViewModelMapper.ValidateTimes(Date, StartTime, EndTime);
}
