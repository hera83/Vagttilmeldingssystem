namespace web.ViewModels.Events;

public class IndexEventsViewModel
{
    public List<EventListItemViewModel> Events { get; set; } = [];
    public string? Query { get; set; }
    public bool ShowArchived { get; set; }
    public int ActiveCount { get; set; }
    public int ArchivedCount { get; set; }
}
