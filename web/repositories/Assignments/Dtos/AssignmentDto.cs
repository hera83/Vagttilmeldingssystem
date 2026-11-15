using web.Data.Models;

namespace web.Repositories.Assignments.Dtos;

public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid UserId { get; set; }
    public AssignmentSource Source { get; set; }
    public bool IsActive { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
