namespace web.Data.Models;

/// <summary>En udgående besked i køen (push nu, e-mail og påmindelser senere). NotificationWorker sender den.</summary>
public class Notification : EntityBase
{
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; }

    /// <summary>Hvad beskeden handler om, fx "ShiftsOpened" eller "Test".</summary>
    public string Kind { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Relativ adresse i appen, som beskeden åbner, fx "/Signup".</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Unik nøgle, så samme besked aldrig lægges i køen to gange (også efter en genstart).</summary>
    public string DedupKey { get; set; } = string.Empty;

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
}
