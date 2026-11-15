namespace web.ViewModels.Signup;

/// <summary>"Ledige vagter": GET viser, POST (Enroll) modtager de valgte vagter.</summary>
public class IndexSignupViewModel
{
    /// <summary>De valgte vagter (afkrydsningsfelterne).</summary>
    public List<Guid> ShiftIds { get; set; } = [];

    /// <summary>Valgt begivenhed i filteret, eller null for alle.</summary>
    public Guid? EventFilter { get; set; }

    /// <summary>Kun til visning.</summary>
    public bool ProfileComplete { get; set; }

    /// <summary>Kun til visning.</summary>
    public List<SignupEventCardViewModel> Events { get; set; } = [];

    /// <summary>Kun til visning.</summary>
    public List<SignupShiftRowViewModel> Shifts { get; set; } = [];

    /// <summary>Kun til visning: antal valgte, der vises i panelet.</summary>
    public int SelectedCount => Shifts.Count(x => x.Selected);
}
