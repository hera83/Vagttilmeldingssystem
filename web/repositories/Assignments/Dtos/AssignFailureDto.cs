namespace web.Repositories.Assignments.Dtos;

public class AssignFailureDto
{
    public Guid ShiftId { get; set; }
    public AssignFailureReason Reason { get; set; }
}
