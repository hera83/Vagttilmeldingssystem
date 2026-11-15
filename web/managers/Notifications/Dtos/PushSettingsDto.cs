namespace web.Managers.Notifications.Dtos;

public class PushSettingsDto
{
    public bool Enabled { get; set; }

    /// <summary>Den offentlige VAPID-nøgle til browserens PushManager.subscribe(). Tom, når push ikke er sat op.</summary>
    public string PublicKey { get; set; } = string.Empty;
}
