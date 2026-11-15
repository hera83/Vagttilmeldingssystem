namespace web.Infrastructure.Notifications;

/// <summary>
/// Antal ulæste i systemets postkasse (singleton). Opdateres af MailboxPollWorker og når Indbakke vises.
/// Dashboardet læser herfra, så det aldrig selv kalder IMAP og aldrig vælter, hvis postkassen ikke kan nås.
/// </summary>
public sealed class MailboxStatus
{
    private readonly Lock _lock = new();
    private MailboxSnapshot _snapshot = new();

    public MailboxSnapshot Current
    {
        get
        {
            lock (_lock)
            {
                return _snapshot;
            }
        }
    }

    public void Configured(bool configured)
    {
        lock (_lock)
        {
            _snapshot = _snapshot with { IsConfigured = configured };
        }
    }

    public void Succeeded(int unreadCount, DateTime checkedAtUtc)
    {
        lock (_lock)
        {
            _snapshot = _snapshot with { IsConfigured = true, UnreadCount = unreadCount, CheckedAt = checkedAtUtc, Error = null };
        }
    }

    public void Failed(string error, DateTime checkedAtUtc)
    {
        lock (_lock)
        {
            _snapshot = _snapshot with { IsConfigured = true, CheckedAt = checkedAtUtc, Error = error };
        }
    }
}
