namespace web.Infrastructure.Notifications;

/// <summary>
/// Status for ReminderWorker (singleton): seneste og næste scanning, antal beskeder og seneste fejl.
/// Vises på admin-dashboardet (step 13). Nulstilles ved genstart (data om beskederne ligger i Notifications).
/// </summary>
public sealed class ReminderWorkerStatus
{
    private readonly Lock _lock = new();
    private ReminderWorkerSnapshot _snapshot = new();

    public ReminderWorkerSnapshot Current
    {
        get
        {
            lock (_lock)
            {
                return _snapshot;
            }
        }
    }

    public void Configure(bool enabled, TimeSpan interval)
    {
        lock (_lock)
        {
            _snapshot = _snapshot with { Enabled = enabled, Interval = interval };
        }
    }

    public void Completed(DateTime startedUtc, DateTime completedUtc, int dueCount, int queuedCount, DateTime nextRunUtc)
    {
        lock (_lock)
        {
            _snapshot = _snapshot with
            {
                LastRunStartedAt = startedUtc,
                LastRunCompletedAt = completedUtc,
                LastDueCount = dueCount,
                LastQueuedCount = queuedCount,
                TotalQueuedCount = _snapshot.TotalQueuedCount + queuedCount,
                RunCount = _snapshot.RunCount + 1,
                NextRunAt = nextRunUtc
            };
        }
    }

    public void Failed(DateTime startedUtc, string error, DateTime nextRunUtc)
    {
        lock (_lock)
        {
            _snapshot = _snapshot with
            {
                LastRunStartedAt = startedUtc,
                LastErrorAt = startedUtc,
                LastError = error,
                RunCount = _snapshot.RunCount + 1,
                NextRunAt = nextRunUtc
            };
        }
    }
}
