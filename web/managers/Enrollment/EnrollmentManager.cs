using web.Data.Models;
using web.Managers.Enrollment.Dtos;
using web.Managers.Enrollment.Interfaces;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Shifts.Interfaces;

namespace web.Managers.Enrollment;

public class EnrollmentManager(
    IShiftRepository shiftRepository,
    IAssignmentRepository assignmentRepository,
    IProfileRepository profileRepository) : IEnrollmentManager
{
    public async Task<AvailableOverviewDto> GetAvailableAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var open = await shiftRepository.GetOpenAsync(DateTime.UtcNow, cancellationToken);
        var taken = await assignmentRepository.GetActiveCountsAsync(open.Select(x => x.Id), cancellationToken);
        var mine = (await assignmentRepository.GetActiveForUserAsync(userId, cancellationToken)).Select(x => x.ShiftId).ToHashSet();

        return new AvailableOverviewDto
        {
            Shifts = open.Select(x => new AvailableShiftDto
            {
                Shift = x,
                Taken = taken.GetValueOrDefault(x.Id),
                IsMine = mine.Contains(x.Id)
            }).ToList(),
            ProfileComplete = await profileRepository.IsCompleteAsync(userId, cancellationToken)
        };
    }

    public async Task<EnrollResultDto> EnrollAsync(Guid userId, IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        if (shiftIds.Count == 0)
        {
            return new EnrollResultDto { Status = EnrollStatus.NothingSelected };
        }

        // Serveren håndhæver profilkravet, uanset hvad browseren viser (krav K02).
        if (!await profileRepository.IsCompleteAsync(userId, cancellationToken))
        {
            return new EnrollResultDto { Status = EnrollStatus.ProfileIncomplete };
        }

        var result = await assignmentRepository.CreateManyAsync(new CreateAssignmentsDto
        {
            UserId = userId,
            ShiftIds = shiftIds.ToList(),
            Source = AssignmentSource.Self,
            AssignedByUserId = null,
            OnlyOpen = true
        }, cancellationToken);

        return result.Succeeded
            ? new EnrollResultDto { Status = EnrollStatus.Success, CreatedShiftIds = result.CreatedShiftIds }
            : new EnrollResultDto { Status = EnrollStatus.Conflict, Failures = result.Failures };
    }

    public async Task<HomeOverviewDto> GetHomeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var mine = await assignmentRepository.GetActiveForUserAsync(userId, cancellationToken);
        var upcoming = mine.Where(x => x.EndUtc >= now).ToList();
        var available = await GetAvailableAsync(userId, cancellationToken);

        return new HomeOverviewDto
        {
            ProfileComplete = available.ProfileComplete,
            NextShift = upcoming.FirstOrDefault(),
            UpcomingCount = upcoming.Count,
            AvailableCount = available.Shifts.Count(x => !x.IsMine && x.Free > 0)
        };
    }
}
