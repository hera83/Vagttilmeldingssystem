using System.Linq.Expressions;
using web.Data.Models;
using web.Repositories.Assignments.Dtos;

namespace web.Repositories.Assignments.Mappers;

public static class AssignmentMapper
{
    public static readonly Expression<Func<Assignment, AssignmentDto>> ToDtoExpression = x => new AssignmentDto
    {
        Id = x.Id,
        ShiftId = x.ShiftId,
        UserId = x.UserId,
        Source = x.Source,
        IsActive = x.IsActive,
        AssignedByUserId = x.AssignedByUserId,
        CreatedAt = x.CreatedAt,
        CancelledAt = x.CancelledAt
    };

    private static readonly Func<Assignment, AssignmentDto> CompiledToDto = ToDtoExpression.Compile();

    public static AssignmentDto ToDto(this Assignment entity) => CompiledToDto(entity);

    public static Assignment ToEntity(this CreateAssignmentsDto dto, Guid shiftId) => new()
    {
        ShiftId = shiftId,
        UserId = dto.UserId,
        Source = dto.Source,
        AssignedByUserId = dto.AssignedByUserId,
        IsActive = true
    };
}
