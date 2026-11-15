namespace web.Repositories.Assignments.Dtos;

/// <summary>En hændelse til "Seneste aktivitet". Kun navne, ingen andre persondata.</summary>
public class AssignmentActivityDto
{
    public AssignmentActivityKind Kind { get; set; }
    public DateTime At { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public Guid ShiftId { get; set; }
    public string ShiftTitle { get; set; } = string.Empty;
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
}
