namespace web.Infrastructure.Notifications;

/// <summary>
/// Absolutte links til appen i mails ud fra App:BaseUrl (fx https://vagter.skolen.dk). Singleton.
/// Mails sendes af NotificationWorker uden en HTTP-request, så adressen kan ikke tages fra requesten.
/// </summary>
public sealed class AppLinks(IConfiguration configuration)
{
    public string? BaseUrl
    {
        get
        {
            var value = configuration["App:BaseUrl"]?.Trim().TrimEnd('/');
            return Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
                ? value
                : null;
        }
    }

    public bool IsConfigured => BaseUrl is not null;

    /// <summary>Det absolutte link, eller null, hvis App:BaseUrl ikke er sat (mailen sendes så uden link).</summary>
    public string? TryAbsolute(string relativePath) =>
        BaseUrl is { } baseUrl ? baseUrl + "/" + relativePath.TrimStart('/') : null;

    /// <summary>Det absolutte link. Kaster, hvis App:BaseUrl ikke er sat (links med engangskoder kræver den).</summary>
    public string Absolute(string relativePath) =>
        TryAbsolute(relativePath) ?? throw new InvalidOperationException("App:BaseUrl er ikke sat. Links i e-mails kræver den.");
}
