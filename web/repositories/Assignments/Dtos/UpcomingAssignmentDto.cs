namespace web.Repositories.Assignments.Dtos;

/// <summary>En aktiv tildeling hos en aktiv bruger med vagtens tidspunkt (til påmindelser).</summary>
public class UpcomingAssignmentDto
{
    public Guid AssignmentId { get; set; }
    public Guid UserId { get; set; }
    public Guid ShiftId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventLocation { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}
