namespace web.Data.Models;

/// <summary>En enheds push-abonnement (web push). Abonnementet er brugerens samtykke til push på den enhed.</summary>
public class PushSubscription : EntityBase
{
    public Guid UserId { get; set; }

    /// <summary>Push-tjenestens adresse for enheden. Unik: logger en anden bruger ind på enheden og slår push til, flyttes abonnementet.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public DateTime? LastSuccessAt { get; set; }
}
