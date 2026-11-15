namespace web.ViewModels.Dashboard;

public class IndexDashboardViewModel
{
    // KPI-kort
    public int ActiveEventCount { get; set; }
    public int OpenShiftCount { get; set; }
    public int OpenFreeSeats { get; set; }
    public int OpenShiftsWithFreeSeats { get; set; }
    public int DraftShiftCount { get; set; }
    public int TakenSeats { get; set; }
    public int Capacity { get; set; }
    public int OccupancyPercent { get; set; }
    public int RemindersSentLast7Days { get; set; }
    public int MessagesSentLast7Days { get; set; }
    public int IncompleteProfileCount { get; set; }
    public int ActiveUserCount { get; set; }

    public List<DashboardEventViewModel> UpcomingEvents { get; set; } = [];
    public List<DashboardEventViewModel> EventsWithDrafts { get; set; } = [];
    public List<DashboardActivityViewModel> Activity { get; set; } = [];

    // Beskeder og påmindelser
    public bool RemindersEnabled { get; set; }
    public string ReminderIntervalText { get; set; } = string.Empty;
    public string LastScanText { get; set; } = string.Empty;
    public string NextScanText { get; set; } = string.Empty;
    public int LastScanQueued { get; set; }
    public string? ReminderError { get; set; }
    public int PendingCount { get; set; }
    public int FailedLast7Days { get; set; }
    public string? LatestFailureText { get; set; }
    public bool PushConfigured { get; set; }
    public bool MailConfigured { get; set; }
}
