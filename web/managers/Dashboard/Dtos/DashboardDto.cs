using web.Infrastructure.Notifications;
using web.Repositories.Notifications.Dtos;
using web.Repositories.Shifts.Dtos;

namespace web.Managers.Dashboard.Dtos;

/// <summary>Alt til admin-dashboardet. Kun tal, der kan beregnes af data (ingen trendtal).</summary>
public class DashboardDto
{
    public DateTime NowUtc { get; set; }
    public int ActiveEventCount { get; set; }
    public ShiftOverviewDto Shifts { get; set; } = new();
    public int ActiveUserCount { get; set; }
    public int IncompleteProfileCount { get; set; }
    public List<DashboardEventDto> UpcomingEvents { get; set; } = [];

    /// <summary>Ikke-arkiverede, ikke afsluttede begivenheder med kladder (genvej til Åbn for tilmelding).</summary>
    public List<DashboardEventDto> EventsWithDrafts { get; set; } = [];

    public List<DashboardActivityDto> Activity { get; set; } = [];
    public NotificationStatsDto Notifications { get; set; } = new();
    public ReminderWorkerSnapshot Reminders { get; set; } = new();
    public MailboxSnapshot Mailbox { get; set; } = new();
    public bool PushConfigured { get; set; }
    public bool MailConfigured { get; set; }
}
