namespace web.ViewModels.MyShifts;

/// <summary>Månedskalender (mandag først) med markering af dage, hvor brugeren har vagter.</summary>
public class MyShiftsCalendarViewModel
{
    public string MonthTitle { get; set; } = string.Empty;
    public string PreviousMonth { get; set; } = string.Empty;
    public string NextMonth { get; set; } = string.Empty;
    public List<List<CalendarDayViewModel>> Weeks { get; set; } = [];
}
