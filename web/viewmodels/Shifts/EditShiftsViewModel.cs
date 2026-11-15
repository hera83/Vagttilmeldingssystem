namespace web.ViewModels.Shifts;

public class EditShiftsViewModel : ShiftFormViewModel
{
    /// <summary>Kun til visning. Id'et kommer fra routen.</summary>
    public Guid Id { get; set; }

    /// <summary>Kun til visning.</summary>
    public bool IsOpen { get; set; }
}
