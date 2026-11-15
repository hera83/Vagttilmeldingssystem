using web.Data.Models;

namespace web.Repositories.Assignments.Dtos;

/// <summary>
/// En vagt, brugeren selv har (aktiv tildeling), med begivenhedens navn og sted.
/// Indeholder bevidst intet om andre vagter i begivenheden eller om andre brugere.
/// </summary>
public class MyShiftDto
{
    public Guid AssignmentId { get; set; }
    public Guid ShiftId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public ShiftStatus ShiftStatus { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string EventLocation { get; set; } = string.Empty;
    public AssignmentSource Source { get; set; }
    public DateTime AssignedAt { get; set; }
}
