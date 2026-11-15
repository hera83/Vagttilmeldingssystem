using web.Managers.Notifications.Dtos;

namespace web.Managers.Notifications.Interfaces;

public interface INotificationManager
{
    Task<PushSettingsDto> GetPushSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Gemmer enhedens push-abonnement for brugeren.</summary>
    Task<SubscribePushResult> SubscribeAsync(Guid userId, SubscribePushDto dto, CancellationToken cancellationToken = default);

    /// <summary>Sletter brugerens eget abonnement. false, hvis det ikke fandtes.</summary>
    Task<bool> UnsubscribeAsync(Guid userId, string endpoint, CancellationToken cancellationToken = default);

    /// <summary>Lægger en testbesked til brugerens enheder i køen (højst én pr. minut) og vækker workeren.</summary>
    Task<QueueTestResult> QueueTestAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Finder nye frigivelser og lægger én generisk besked pr. modtager i køen. Returnerer antal nye beskeder.</summary>
    Task<int> QueueOpenedShiftsAsync(CancellationToken cancellationToken = default);

    /// <summary>Sender ventende beskeder (højst BatchSize ad gangen). Returnerer antal behandlede.</summary>
    Task<int> SendDueAsync(CancellationToken cancellationToken = default);
}
