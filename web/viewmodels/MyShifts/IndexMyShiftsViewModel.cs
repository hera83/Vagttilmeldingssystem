namespace web.ViewModels.MyShifts;

public class IndexMyShiftsViewModel
{
    public List<MyShiftItemViewModel> Upcoming { get; set; } = [];
    public List<MyShiftItemViewModel> Past { get; set; } = [];
    public MyShiftsCalendarViewModel Calendar { get; set; } = new();
}
