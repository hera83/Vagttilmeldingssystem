namespace web.Infrastructure.Notifications;

/// <summary>Et øjebliksbillede af ReminderWorkerStatus. Tidspunkter i UTC.</summary>
public sealed record ReminderWorkerSnapshot
{
    public bool Enabled { get; init; }
    public TimeSpan Interval { get; init; }
    public DateTime? LastRunStartedAt { get; init; }
    public DateTime? LastRunCompletedAt { get; init; }
    public DateTime? NextRunAt { get; init; }
    public int LastDueCount { get; init; }
    public int LastQueuedCount { get; init; }
    public int TotalQueuedCount { get; init; }
    public int RunCount { get; init; }
    public DateTime? LastErrorAt { get; init; }
    public string? LastError { get; init; }
}
