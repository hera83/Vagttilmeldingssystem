namespace web.ViewModels.Events;

public class EventListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PeriodText { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
}
