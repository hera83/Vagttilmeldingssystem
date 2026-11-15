namespace web.ViewModels.Signup;

public class SignupEventCardViewModel
{
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public int FreeShiftCount { get; set; }
    public bool IsSelected { get; set; }
}
