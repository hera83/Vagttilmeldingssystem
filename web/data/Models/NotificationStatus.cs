namespace web.Data.Models;

/// <summary>Status for en besked i køen.</summary>
public enum NotificationStatus
{
    /// <summary>Venter på at blive sendt (også efter et fejlet forsøg, se NextAttemptAt).</summary>
    Pending,

    /// <summary>Sendt til mindst én modtager (enhed eller e-mail).</summary>
    Sent,

    /// <summary>Opgivet efter flere fejlede forsøg. Årsagen står i LastError.</summary>
    Failed,

    /// <summary>Ikke sendt, fordi der ikke var nogen modtager (fx intet abonnement), eller kanalen ikke er sat op.</summary>
    Skipped
}
