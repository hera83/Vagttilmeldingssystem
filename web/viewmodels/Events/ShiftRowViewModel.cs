namespace web.ViewModels.Events;

/// <summary>En vagt i tabellen på begivenhedens side (admin).</summary>
public class ShiftRowViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Taken { get; set; }
    public bool IsOpen { get; set; }
}
