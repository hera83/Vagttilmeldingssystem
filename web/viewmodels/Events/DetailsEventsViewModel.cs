namespace web.ViewModels.Events;

public class DetailsEventsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PeriodText { get; set; } = string.Empty;
    public bool IsArchived { get; set; }

    public List<ShiftRowViewModel> Shifts { get; set; } = [];
    public int DraftCount { get; set; }
    public int OpenCount { get; set; }

    /// <summary>null = alle, "Draft" eller "Open".</summary>
    public string? StatusFilter { get; set; }

    public int TotalCount => DraftCount + OpenCount;
}
