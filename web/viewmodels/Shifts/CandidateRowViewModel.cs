namespace web.ViewModels.Shifts;

public class CandidateRowViewModel
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsCurrentUser { get; set; }
}
