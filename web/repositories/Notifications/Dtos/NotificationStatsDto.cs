namespace web.Repositories.Notifications.Dtos;

/// <summary>Køens status til dashboardet.</summary>
public class NotificationStatsDto
{
    public int SentSince { get; set; }
    public int RemindersSentSince { get; set; }
    public int PendingCount { get; set; }
    public int FailedSince { get; set; }

    /// <summary>Den seneste besked, der er opgivet (Failed), eller null.</summary>
    public NotificationDto? LatestFailure { get; set; }
}
