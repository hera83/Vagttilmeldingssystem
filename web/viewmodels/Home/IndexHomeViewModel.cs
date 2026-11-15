namespace web.ViewModels.Home;

public class IndexHomeViewModel
{
    public bool ProfileComplete { get; set; }
    public int UpcomingCount { get; set; }
    public int AvailableCount { get; set; }

    public bool HasNextShift { get; set; }
    public Guid NextShiftId { get; set; }
    public string NextShiftTitle { get; set; } = string.Empty;
    public string NextShiftEventName { get; set; } = string.Empty;
    public string NextShiftLocation { get; set; } = string.Empty;
    public string NextShiftDateText { get; set; } = string.Empty;
    public string NextShiftTimeText { get; set; } = string.Empty;
}
