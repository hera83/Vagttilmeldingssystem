using web.Data.Models;

namespace web.Repositories.Assignments.Dtos;

/// <summary>Én af brugerens tildelinger, også afmeldte (dataudtræk, GDPR art. 15).</summary>
public class AssignmentHistoryDto
{
    public string EventName { get; set; } = string.Empty;
    public string ShiftTitle { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public AssignmentSource Source { get; set; }
    public bool IsActive { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
