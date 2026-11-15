namespace web.Services.Push;

/// <summary>
/// Kun abonnementer hos de kendte push-tjenester accepteres. Serveren sender en POST til abonnementets adresse,
/// så en vilkårlig adresse ville kunne bruges til at få serveren til at kalde interne systemer (SSRF).
/// </summary>
public static class PushEndpoints
{
    /// <summary>Værtsnavne (og deres underdomæner) for Chrome/Edge/Android (FCM), Firefox, Windows (WNS) og Apple.</summary>
    public static readonly IReadOnlyList<string> AllowedHosts =
    [
        "fcm.googleapis.com",
        "android.googleapis.com",
        "push.services.mozilla.com",
        "notify.windows.com",
        "push.apple.com"
    ];

    public static bool IsAllowed(string? endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        uri.IsDefaultPort &&
        AllowedHosts.Any(host => uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase) ||
                                 uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase));

    /// <summary>Værtsnavnet til fejlbeskeder og log. Selve adressen er en hemmelighed for enheden og logges aldrig.</summary>
    public static string HostOf(string endpoint) =>
        Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ? uri.Host : "ukendt";
}
