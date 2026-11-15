namespace web.ViewModels.Signup;

public class SignupShiftRowViewModel
{
    public Guid ShiftId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public int Free { get; set; }
    public int Capacity { get; set; }
    public bool IsMine { get; set; }
    public bool IsFull => !IsMine && Free == 0;
    public bool Selected { get; set; }
}
