namespace web.Infrastructure.Realtime;

/// <summary>Sender realtidssignaler til åbne sider. Kaldes af managers efter commit. Kaster aldrig.</summary>
public interface IShiftUpdatesNotifier
{
    Task PublishAsync(ShiftUpdate update, CancellationToken cancellationToken = default);
}
