using web.Data.Models;

namespace web.Repositories.Assignments.Dtos;

/// <summary>En aktiv tildeling i en begivenhed til deltagerlisten (kun admin).</summary>
public class EventParticipantDto
{
    public Guid ShiftId { get; set; }
    public string ShiftTitle { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public AssignmentSource Source { get; set; }
}
