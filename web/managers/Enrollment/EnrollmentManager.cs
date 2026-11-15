using web.Data.Models;
using web.Extensions;
using web.Infrastructure.Notifications;
using web.Infrastructure.Realtime;
using web.Managers.Enrollment.Dtos;
using web.Managers.Enrollment.Interfaces;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Notifications.Dtos;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Shifts.Interfaces;

namespace web.Managers.Enrollment;

public class EnrollmentManager(
    IShiftRepository shiftRepository,
    IAssignmentRepository assignmentRepository,
    IProfileRepository profileRepository,
    IShiftUpdatesNotifier notifier,
    INotificationRepository notificationRepository,
    NotificationSignal notificationSignal,
    AppLinks appLinks) : IEnrollmentManager
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

        if (!result.Succeeded)
        {
            return new EnrollResultDto { Status = EnrollStatus.Conflict, Failures = result.Failures };
        }

        // Efter commit: færre ledige pladser på åbne vagter (alle) og brugerens egne vagter (brugeren).
        var eventIds = await shiftRepository.GetEventIdsAsync(result.CreatedShiftIds, cancellationToken);
        foreach (var group in result.CreatedShiftIds.GroupBy(id => eventIds.GetValueOrDefault(id)))
        {
            await notifier.PublishAsync(new ShiftUpdate
            {
                EventId = group.Key,
                ShiftIds = group.ToList(),
                IsPublic = true,
                AffectedUserIds = [userId]
            }, cancellationToken);
        }

        await QueueConfirmationAsync(userId, result.CreatedShiftIds, cancellationToken);
        return new EnrollResultDto { Status = EnrollStatus.Success, CreatedShiftIds = result.CreatedShiftIds };
    }

    /// <summary>Bekræftelsesmail med de vagter, brugeren netop er tilmeldt (kun egne vagter, dansk tid). Sendes via køen.</summary>
    private async Task QueueConfirmationAsync(Guid userId, IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken)
    {
        if (!await profileRepository.GetEmailNotificationsAsync(userId, cancellationToken))
        {
            return;
        }

        var lines = (await assignmentRepository.GetActiveForUserAsync(userId, cancellationToken))
            .Where(x => shiftIds.Contains(x.ShiftId))
            .OrderBy(x => x.StartUtc)
            .Select(x => new MailShiftLine(x.Title, x.EventName,
                $"{x.StartUtc.ToDanishDateText()} kl. {DateTimeExtensions.ToDanishTimeRangeText(x.StartUtc, x.EndUtc)}", x.EventLocation))
            .ToList();
        if (lines.Count == 0)
        {
            return;
        }

        var link = appLinks.TryAbsolute("/MyShifts");
        var mail = MailTemplates.EnrollmentConfirmation(lines, link);
        await notificationRepository.CreateManyAsync(
        [
            new CreateNotificationDto
            {
                UserId = userId,
                Channel = NotificationChannel.Email,
                Kind = NotificationKinds.EnrollmentConfirmation,
                Title = mail.Subject,
                Body = mail.Body,
                Url = link ?? string.Empty,
                DedupKey = $"enroll:{userId:N}:{Guid.NewGuid():N}",
                NotBeforeUtc = DateTime.UtcNow
            }
        ], cancellationToken);
        notificationSignal.Notify();
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
