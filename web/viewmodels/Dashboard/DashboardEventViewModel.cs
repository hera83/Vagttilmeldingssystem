namespace web.ViewModels.Dashboard;

public class DashboardEventViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PeriodText { get; set; } = string.Empty;
    public int ShiftCount { get; set; }
    public int DraftCount { get; set; }
    public int Taken { get; set; }
    public int Capacity { get; set; }
    public int Percent { get; set; }

    /// <summary>"Kladder", "Åben" eller "Ingen vagter".</summary>
    public string StatusText { get; set; } = string.Empty;

    public string StatusBadgeClass { get; set; } = string.Empty;
}
