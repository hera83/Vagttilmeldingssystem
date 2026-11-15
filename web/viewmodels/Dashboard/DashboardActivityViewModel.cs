namespace web.ViewModels.Dashboard;

public class DashboardActivityViewModel
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string WhenText { get; set; } = string.Empty;
    public Guid EventId { get; set; }
}
