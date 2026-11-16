using web.Extensions;
using web.Managers.Dashboard.Dtos;

namespace web.ViewModels.Dashboard;

public static class DashboardViewModelMapper
{
    public static IndexDashboardViewModel ToIndexDashboardViewModel(this DashboardDto dto)
    {
        var reminders = dto.Reminders;
        return new IndexDashboardViewModel
        {
            ActiveEventCount = dto.ActiveEventCount,
            OpenShiftCount = dto.Shifts.OpenShiftCount,
            OpenFreeSeats = dto.Shifts.OpenFreeSeats,
            OpenShiftsWithFreeSeats = dto.Shifts.OpenShiftsWithFreeSeats,
            DraftShiftCount = dto.Shifts.DraftShiftCount,
            TakenSeats = dto.Shifts.UpcomingTaken,
            Capacity = dto.Shifts.UpcomingCapacity,
            OccupancyPercent = Percent(dto.Shifts.UpcomingTaken, dto.Shifts.UpcomingCapacity),
            RemindersSentLast7Days = dto.Notifications.RemindersSentSince,
            MessagesSentLast7Days = dto.Notifications.SentSince,
            IncompleteProfileCount = dto.IncompleteProfileCount,
            ActiveUserCount = dto.ActiveUserCount,
            MailboxUnreadText = !dto.Mailbox.IsConfigured ? "–" : dto.Mailbox.Error is not null ? "!" : dto.Mailbox.UnreadCount?.ToString() ?? "…",
            MailboxSubText = !dto.Mailbox.IsConfigured ? "Webmail er ikke sat op"
                : dto.Mailbox.Error is not null ? "Postkassen kunne ikke nås"
                : dto.Mailbox.UnreadCount is null ? "Ikke tjekket endnu"
                : $"i Indbakke · tjekket {dto.Mailbox.CheckedAt?.ToDanishRelativeText(dto.NowUtc).ToLowerInvariant()}",
            MailboxHasUnread = dto.Mailbox.UnreadCount > 0,
            UpcomingEvents = dto.UpcomingEvents.Select(ToEvent).ToList(),
            EventsWithDrafts = dto.EventsWithDrafts.Select(ToEvent).ToList(),
            Activity = dto.Activity.Select(x => ToActivity(x, dto.NowUtc)).ToList(),
            RemindersEnabled = reminders.Enabled,
            ReminderIntervalText = IntervalText(reminders.Interval),
            LastScanText = reminders.LastRunStartedAt?.ToDanishRelativeText(dto.NowUtc) ?? "Ikke kørt endnu",
            NextScanText = reminders.NextRunAt?.ToDanishRelativeText(dto.NowUtc) ?? "–",
            LastScanQueued = reminders.LastQueuedCount,
            ReminderError = reminders.LastError is null ? null : $"{reminders.LastErrorAt?.ToDanishRelativeText(dto.NowUtc)}: {reminders.LastError}",
            PendingCount = dto.Notifications.PendingCount,
            FailedLast7Days = dto.Notifications.FailedSince,
            LatestFailureText = dto.Notifications.LatestFailure is { } failure
                ? $"{failure.CreatedAt.ToDanishRelativeText(dto.NowUtc)} ({ChannelText(failure.Channel.ToString())}): {failure.LastError}"
                : null,
            PushConfigured = dto.PushConfigured,
            MailConfigured = dto.MailConfigured
        };
    }

    private static DashboardEventViewModel ToEvent(DashboardEventDto x) => new()
    {
        Id = x.Event.Id,
        Name = x.Event.Name,
        Location = x.Event.Location,
        PeriodText = DateTimeExtensions.ToDanishPeriodText(x.Event.StartDate, x.Event.EndDate),
        ShiftCount = x.ShiftCount,
        DraftCount = x.DraftCount,
        Taken = x.Taken,
        Capacity = x.Capacity,
        Percent = Percent(x.Taken, x.Capacity),
        StatusText = x.ShiftCount == 0 ? "Ingen vagter" : x.DraftCount > 0 ? (x.OpenCount > 0 ? "Åben + kladder" : "Kladder") : "Åben",
        StatusBadgeClass = x.ShiftCount == 0 ? "badge-neutral" : x.DraftCount > 0 ? "badge-draft" : "badge-open"
    };

    private static DashboardActivityViewModel ToActivity(DashboardActivityDto x, DateTime nowUtc) => new()
    {
        EventId = x.EventId,
        WhenText = x.At.ToDanishRelativeText(nowUtc),
        Icon = x.Kind switch
        {
            DashboardActivityKind.Enrolled => "user-plus",
            DashboardActivityKind.Assigned => "users",
            DashboardActivityKind.Removed => "x",
            _ => "play"
        },
        Title = x.Kind switch
        {
            DashboardActivityKind.Enrolled => "Ny tilmelding",
            DashboardActivityKind.Assigned => "Tildelt af administrator",
            DashboardActivityKind.Removed => "Fjernet fra vagt",
            _ => "Åbnet for tilmelding"
        },
        Text = x.Kind switch
        {
            DashboardActivityKind.Enrolled => $"{x.UserDisplayName} tilmeldte sig »{x.ShiftTitle}« ({x.EventName}).",
            DashboardActivityKind.Assigned => $"{x.UserDisplayName} blev sat på »{x.ShiftTitle}« ({x.EventName}).",
            DashboardActivityKind.Removed => $"{x.UserDisplayName} blev fjernet fra »{x.ShiftTitle}« ({x.EventName}).",
            _ => $"{x.ShiftCount} {(x.ShiftCount == 1 ? "vagt" : "vagter")} i {x.EventName}" + (x.ByName is null ? "." : $" af {x.ByName}.")
        }
    };

    private static int Percent(int part, int total) => total <= 0 ? 0 : (int)Math.Round(100.0 * part / total);

    /// <summary>"hver time", "hver 2. time", "hvert minut", "hvert 15. minut" eller "hvert 20. sekund" (fundet i step 17: under et minut stod der "hvert 0. minut").</summary>
    public static string IntervalText(TimeSpan interval)
    {
        var seconds = (long)Math.Round(interval.TotalSeconds);
        return seconds switch
        {
            >= 3600 when seconds % 3600 == 0 => seconds == 3600 ? "hver time" : $"hver {seconds / 3600}. time",
            >= 60 when seconds % 60 == 0 => seconds == 60 ? "hvert minut" : $"hvert {seconds / 60}. minut",
            <= 1 => "hvert sekund",
            _ => $"hvert {seconds}. sekund"
        };
    }

    private static string ChannelText(string channel) => channel == "Email" ? "e-mail" : "push";
}
