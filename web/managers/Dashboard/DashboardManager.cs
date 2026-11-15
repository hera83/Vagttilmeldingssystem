using web.Extensions;
using web.Infrastructure.Notifications;
using web.Managers.Dashboard.Dtos;
using web.Managers.Dashboard.Interfaces;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Events.Interfaces;
using web.Repositories.Notifications.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Shifts.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Services.Mail.Interfaces;
using web.Services.Push.Interfaces;

namespace web.Managers.Dashboard;

/// <summary>Admin-dashboardet: tal, kommende begivenheder, seneste aktivitet og status for beskeder og påmindelser.</summary>
public class DashboardManager(
    IEventRepository eventRepository,
    IShiftRepository shiftRepository,
    IAssignmentRepository assignmentRepository,
    IUserRepository userRepository,
    IProfileRepository profileRepository,
    INotificationRepository notificationRepository,
    IPushService pushService,
    IMailService mailService,
    ReminderWorkerStatus reminderStatus,
    TimeProvider timeProvider) : IDashboardManager
{
    public const int UpcomingEventCount = 6;
    public const int ActivityCount = 8;
    public static readonly TimeSpan StatsPeriod = TimeSpan.FromDays(7);

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now.ToDanishTime());

        var users = (await userRepository.GetAllAsync(cancellationToken)).Where(x => x.IsActive).ToList();
        var complete = await profileRepository.GetCompleteUserIdsAsync(cancellationToken);

        // Alle kommende begivenheder bruges til genvejene. De første vises i tabellen.
        var upcoming = await eventRepository.GetUpcomingAsync(today, 100, cancellationToken);
        var stats = (await shiftRepository.GetEventStatsAsync(upcoming.Select(x => x.Id), cancellationToken)).ToDictionary(x => x.EventId);
        var events = upcoming.Select(x =>
        {
            var s = stats.GetValueOrDefault(x.Id);
            return new DashboardEventDto
            {
                Event = x,
                ShiftCount = s?.ShiftCount ?? 0,
                DraftCount = s?.DraftCount ?? 0,
                OpenCount = s?.OpenCount ?? 0,
                Capacity = s?.Capacity ?? 0,
                Taken = s?.Taken ?? 0
            };
        }).ToList();

        return new DashboardDto
        {
            NowUtc = now,
            ActiveEventCount = (await eventRepository.GetCountsAsync(cancellationToken)).Active,
            Shifts = await shiftRepository.GetOverviewAsync(now, cancellationToken),
            ActiveUserCount = users.Count,
            IncompleteProfileCount = users.Count(x => !complete.Contains(x.Id)),
            UpcomingEvents = events.Take(UpcomingEventCount).ToList(),
            EventsWithDrafts = events.Where(x => x.DraftCount > 0).ToList(),
            Activity = await GetActivityAsync(cancellationToken),
            Notifications = await notificationRepository.GetStatsAsync(now - StatsPeriod, cancellationToken),
            Reminders = reminderStatus.Current,
            PushConfigured = await pushService.GetPublicKeyAsync(cancellationToken) is not null,
            MailConfigured = await mailService.IsConfiguredAsync(cancellationToken)
        };
    }

    private async Task<List<DashboardActivityDto>> GetActivityAsync(CancellationToken cancellationToken)
    {
        var assignments = (await assignmentRepository.GetRecentActivityAsync(ActivityCount, cancellationToken)).Select(x => new DashboardActivityDto
        {
            Kind = x.Kind switch
            {
                AssignmentActivityKind.Enrolled => DashboardActivityKind.Enrolled,
                AssignmentActivityKind.Assigned => DashboardActivityKind.Assigned,
                _ => DashboardActivityKind.Removed
            },
            At = x.At,
            EventId = x.EventId,
            EventName = x.EventName,
            UserDisplayName = x.UserDisplayName,
            ShiftTitle = x.ShiftTitle
        });
        var openings = (await shiftRepository.GetRecentOpeningsAsync(ActivityCount, cancellationToken)).Select(x => new DashboardActivityDto
        {
            Kind = DashboardActivityKind.Opened,
            At = x.OpenedAt,
            EventId = x.EventId,
            EventName = x.EventName,
            ShiftCount = x.ShiftCount,
            ByName = x.OpenedByName
        });

        return assignments.Concat(openings).OrderByDescending(x => x.At).Take(ActivityCount).ToList();
    }
}
