namespace web.ViewModels.Shifts;

/// <summary>Administratorens side til at tildele og fjerne personer på en vagt.</summary>
public class AssignShiftsViewModel
{
    public Guid ShiftId { get; set; }
    public string ShiftTitle { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Taken { get; set; }
    public bool IsOpen { get; set; }
    public bool IsFull => Taken >= Capacity;
    public string? Search { get; set; }
    public List<AssigneeRowViewModel> Assignees { get; set; } = [];
    public List<CandidateRowViewModel> Candidates { get; set; } = [];
    public int CandidateTotal { get; set; }
}
