using web.Repositories.Assignments.Dtos;

namespace web.Managers.Enrollment.Dtos;

public class EnrollResultDto
{
    public EnrollStatus Status { get; set; }
    public List<Guid> CreatedShiftIds { get; set; } = [];
    public List<AssignFailureDto> Failures { get; set; } = [];
}
