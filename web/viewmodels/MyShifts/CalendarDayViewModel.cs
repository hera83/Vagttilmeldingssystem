namespace web.ViewModels.MyShifts;

public class CalendarDayViewModel
{
    public int Day { get; set; }
    public string DateText { get; set; } = string.Empty;
    public bool InMonth { get; set; }
    public bool IsToday { get; set; }
    public int UpcomingCount { get; set; }
    public int PastCount { get; set; }
}
