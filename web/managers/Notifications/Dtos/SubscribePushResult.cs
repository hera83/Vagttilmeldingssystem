namespace web.Managers.Notifications.Dtos;

public enum SubscribePushResult
{
    Success,

    /// <summary>Adressen er ikke en kendt push-tjeneste (https).</summary>
    InvalidEndpoint,

    /// <summary>Push er ikke sat op på serveren.</summary>
    PushDisabled
}
