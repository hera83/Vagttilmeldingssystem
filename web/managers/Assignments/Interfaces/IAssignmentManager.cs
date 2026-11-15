using web.Managers.Assignments.Dtos;

namespace web.Managers.Assignments.Interfaces;

public interface IAssignmentManager
{
    Task<AssignOverviewDto?> GetAssignOverviewAsync(Guid shiftId, string? search, CancellationToken cancellationToken = default);

    /// <summary>Administratoren tildeler en bruger (også sig selv) en vagt, også en kladde. Kapaciteten gælder.</summary>
    Task<AssignUserResult> AssignAsync(Guid shiftId, Guid userId, Guid assignedByUserId, CancellationToken cancellationToken = default);

    /// <summary>Administratoren fjerner en bruger fra en vagt. Pladsen bliver ledig igen.</summary>
    Task<UnassignResult> UnassignAsync(Guid shiftId, Guid assignmentId, Guid removedByUserId, CancellationToken cancellationToken = default);
}
