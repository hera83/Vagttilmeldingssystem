namespace web.Infrastructure.Notifications;

/// <summary>
/// Vækker NotificationWorker med det samme (fx efter en frigivelse), så den ikke venter på sit næste interval.
/// Singleton. Flere signaler, før workeren når at køre, bliver til ét gennemløb.
/// </summary>
public sealed class NotificationSignal
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void Notify()
    {
        try
        {
            _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Der er allerede et ventende signal.
        }
    }

    /// <summary>Venter på et signal eller på timeout. true = signal.</summary>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _signal.WaitAsync(timeout, cancellationToken);
}
