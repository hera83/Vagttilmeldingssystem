using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace web.Infrastructure.Security;

/// <summary>
/// Bag en reverse proxy (step 16): X-Forwarded-For og X-Forwarded-Proto bruges kun fra proxies, appen stoler på.
/// Så ser https-redirect, HSTS, Secure-cookien og rate limiting (pr. IP) klienten og ikke proxyen.
/// Standard (ingen konfiguration): kun proxies på samme maskine (127.0.0.0/8 og ::1).
/// </summary>
public static class ForwardedHeadersConfiguration
{
    public const string KnownProxiesKey = "ForwardedHeaders:KnownProxies";
    public const string KnownNetworksKey = "ForwardedHeaders:KnownNetworks";

    private static readonly char[] Separators = [',', ';', ' '];

    public static void Apply(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        foreach (var value in Split(configuration[KnownProxiesKey]))
        {
            options.KnownProxies.Add(IPAddress.TryParse(value, out var address)
                ? address
                : throw new InvalidOperationException($"{KnownProxiesKey} indeholder \"{value}\", som ikke er en IP-adresse."));
        }

        foreach (var value in Split(configuration[KnownNetworksKey]))
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.TryParse(value, out var network)
                ? network
                : throw new InvalidOperationException($"{KnownNetworksKey} indeholder \"{value}\", som ikke er et netværk som fx 172.16.0.0/12."));
        }
    }

    private static string[] Split(string? value) =>
        (value ?? string.Empty).Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
