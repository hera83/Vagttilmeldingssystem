using web.Data.Models;

namespace web.Repositories.Assignments.Dtos;

/// <summary>En aktiv tildeling på en vagt med brugerens navn (til administratorens liste).</summary>
public class AssigneeDto
{
    public Guid AssignmentId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AssignmentSource Source { get; set; }
    public DateTime CreatedAt { get; set; }
}
