using web.Data.Models;
using web.Infrastructure.Realtime;
using web.Managers.Assignments.Dtos;
using web.Managers.Assignments.Interfaces;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Events.Interfaces;
using web.Repositories.Shifts.Interfaces;
using web.Repositories.Users.Interfaces;

namespace web.Managers.Assignments;

public class AssignmentManager(
    IShiftRepository shiftRepository,
    IEventRepository eventRepository,
    IUserRepository userRepository,
    IAssignmentRepository assignmentRepository,
    IShiftUpdatesNotifier notifier) : IAssignmentManager
{
    public const int MaxCandidates = 50;

    public async Task<AssignOverviewDto?> GetAssignOverviewAsync(Guid shiftId, string? search, CancellationToken cancellationToken = default)
    {
        var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        var item = shift is null ? null : await eventRepository.GetByIdAsync(shift.EventId, cancellationToken);
        if (shift is null || item is null)
        {
            return null;
        }

        var assignees = await assignmentRepository.GetActiveByShiftAsync(shiftId, cancellationToken);
        var assigned = assignees.Select(x => x.UserId).ToHashSet();
        var text = search?.Trim();

        // Få brugere (én klasse eller skole), så filtreringen sker i hukommelsen.
        var candidates = (await userRepository.GetAllAsync(cancellationToken))
            .Where(u => u.IsActive && !assigned.Contains(u.Id))
            .Where(u => string.IsNullOrEmpty(text)
                        || u.DisplayName.Contains(text, StringComparison.CurrentCultureIgnoreCase)
                        || u.UserName.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new AssignOverviewDto
        {
            Event = item,
            Shift = shift,
            Assignees = assignees,
            Candidates = candidates.Take(MaxCandidates).ToList(),
            CandidateTotal = candidates.Count
        };
    }

    public async Task<AssignUserResult> AssignAsync(Guid shiftId, Guid userId, Guid assignedByUserId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return AssignUserResult.UserNotFound;
        }

        if (!user.IsActive)
        {
            return AssignUserResult.UserInactive;
        }

        var result = await assignmentRepository.CreateManyAsync(new CreateAssignmentsDto
        {
            UserId = userId,
            ShiftIds = [shiftId],
            Source = AssignmentSource.Admin,
            AssignedByUserId = assignedByUserId,
            OnlyOpen = false
        }, cancellationToken);

        if (result.Succeeded)
        {
            await PublishAsync(shiftId, userId, cancellationToken);
            return AssignUserResult.Success;
        }

        return result.Failures.FirstOrDefault()?.Reason switch
        {
            AssignFailureReason.AlreadyAssigned => AssignUserResult.AlreadyAssigned,
            AssignFailureReason.Full => AssignUserResult.Full,
            _ => AssignUserResult.ShiftNotFound
        };
    }

    public async Task<UnassignResult> UnassignAsync(Guid shiftId, Guid assignmentId, Guid removedByUserId, CancellationToken cancellationToken = default)
    {
        var assignment = await assignmentRepository.GetByIdAsync(assignmentId, cancellationToken);
        if (assignment is null || assignment.ShiftId != shiftId || !assignment.IsActive)
        {
            return UnassignResult.NotFound;
        }

        if (await assignmentRepository.CancelAsync(assignmentId, removedByUserId, cancellationToken) is null)
        {
            return UnassignResult.NotFound;
        }

        await PublishAsync(shiftId, assignment.UserId, cancellationToken);
        return UnassignResult.Success;
    }

    // Efter commit. En kladdes id sendes kun til administratorer og til den berørte bruger personligt (krav K10).
    private async Task PublishAsync(Guid shiftId, Guid userId, CancellationToken cancellationToken)
    {
        var shift = await shiftRepository.GetByIdAsync(shiftId, cancellationToken);
        if (shift is null)
        {
            return;
        }

        await notifier.PublishAsync(new ShiftUpdate
        {
            EventId = shift.EventId,
            ShiftIds = [shiftId],
            IsPublic = shift.Status == ShiftStatus.Open,
            AffectedUserIds = [userId]
        }, cancellationToken);
    }
}
