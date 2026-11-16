namespace web.Infrastructure.Notifications;

/// <summary>
/// Absolutte links til appen i mails ud fra App:Domain (fx vagter.skolen.dk). Singleton.
/// I drift bliver adressen https://&lt;domæne&gt;, i udvikling http://&lt;domæne&gt;. I udvikling uden App:Domain bruges appens
/// egen http-adresse (urls fra launch-profilen).
/// Adressen læses aldrig fra requesten: mails sendes af baggrundsjob uden en request, og en Host-header fra klienten
/// ville give falske links i mails om glemt adgangskode.
/// </summary>
public sealed class AppLinks(IConfiguration configuration, IHostEnvironment environment)
{
    public const string DomainKey = "App:Domain";

    public string? BaseUrl
    {
        get
        {
            var domain = ParseDomain(configuration[DomainKey]);
            if (environment.IsDevelopment())
            {
                return domain is null ? DevelopmentUrl(configuration["urls"]) : "http://" + domain;
            }

            return domain is null ? null : "https://" + domain;
        }
    }

    public bool IsConfigured => BaseUrl is not null;

    /// <summary>Det absolutte link, eller null, hvis App:Domain ikke er sat (mailen sendes så uden link).</summary>
    public string? TryAbsolute(string relativePath) =>
        BaseUrl is { } baseUrl ? baseUrl + "/" + relativePath.TrimStart('/') : null;

    /// <summary>Det absolutte link. Kaster, hvis App:Domain ikke er sat (links med engangskoder kræver den).</summary>
    public string Absolute(string relativePath) =>
        TryAbsolute(relativePath) ?? throw new InvalidOperationException($"{DomainKey} er ikke sat. Links i e-mails kræver den.");

    /// <summary>
    /// Domænet (evt. med port) i små bogstaver, eller null, hvis værdien er tom. Et foranstillet http:// eller https:// og
    /// en / til sidst fjernes, så en hel adresse også virker. Kaster med nøglens navn ved en sti, query eller brugerinfo.
    /// </summary>
    public static string? ParseDomain(string? value)
    {
        var domain = value?.Trim() ?? string.Empty;
        foreach (var scheme in new[] { "https://", "http://" })
        {
            if (domain.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
            {
                domain = domain[scheme.Length..];
            }
        }

        domain = domain.TrimEnd('/');
        if (domain.Length == 0)
        {
            return null;
        }

        if (!Uri.TryCreate("https://" + domain, UriKind.Absolute, out var uri) ||
            uri.HostNameType is UriHostNameType.Unknown ||
            uri.AbsolutePath != "/" || uri.Query.Length > 0 || uri.Fragment.Length > 0 || uri.UserInfo.Length > 0 ||
            domain.Contains('/') || domain.Contains('?') || domain.Contains('#'))
        {
            throw new InvalidOperationException(
                $"{DomainKey} indeholder \"{value}\", som ikke er et domæne. Skriv kun domænet, fx vagter.skolen.dk.");
        }

        // Port 443 er standard for https og udelades. Andre porte (fx :8443 eller :5180 lokalt) bevares.
        var host = uri.Host.ToLowerInvariant();
        return uri.IsDefaultPort ? host : $"{host}:{uri.Port}";
    }

    // urls fra launch-profilen, fx "https://localhost:7180;http://localhost:5180". http foretrækkes i udvikling.
    // Lytter appen på alle adresser (0.0.0.0, [::], *, +), bruges localhost.
    private static string? DevelopmentUrl(string? urls)
    {
        var candidates = (urls ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(url => url.Replace("://0.0.0.0", "://localhost").Replace("://[::]", "://localhost")
                .Replace("://*", "://localhost").Replace("://+", "://localhost").TrimEnd('/'))
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .ToList();

        return candidates.FirstOrDefault(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
               ?? candidates.FirstOrDefault();
    }
}
