namespace web.ViewModels.Shifts;

public class AssigneeRowViewModel
{
    public Guid AssignmentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsAdminAssigned { get; set; }
    public bool IsCurrentUser { get; set; }
    public string AssignedText { get; set; } = string.Empty;
}
