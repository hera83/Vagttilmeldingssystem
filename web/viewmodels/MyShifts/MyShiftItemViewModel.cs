namespace web.ViewModels.MyShifts;

/// <summary>En vagt i listen under Mine vagter.</summary>
public class MyShiftItemViewModel
{
    public Guid ShiftId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string WeekdayShort { get; set; } = string.Empty;
    public string DayNumber { get; set; } = string.Empty;
    public string MonthShort { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public bool IsAdminAssigned { get; set; }
    public bool IsPast { get; set; }
}
