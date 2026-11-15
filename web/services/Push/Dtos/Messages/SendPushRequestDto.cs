namespace web.Services.Push.Dtos.Messages;

public class SendPushRequestDto
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Relativ adresse i appen, som notifikationen åbner.</summary>
    public string Url { get; set; } = "/";

    /// <summary>Notifikationer med samme tag erstatter hinanden på enheden.</summary>
    public string? Tag { get; set; }

    public PushUrgency Urgency { get; set; } = PushUrgency.Normal;
    public int TimeToLiveSeconds { get; set; } = 24 * 60 * 60;
}
