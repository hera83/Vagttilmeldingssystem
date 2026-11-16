using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace web.Infrastructure.Security;

/// <summary>
/// Bag en reverse proxy med https (step 16 og 19): X-Forwarded-For og X-Forwarded-Proto bruges kun fra proxies, appen stoler på.
/// Så ser https-redirect, HSTS, Secure-cookien, anti-forgery og rate limiting (pr. IP) klienten og ikke proxyen.
/// Uden konfiguration stoles der på samme maskine og på private netværk (Docker, en proxy på serveren eller på LAN).
/// ForwardedHeaders:KnownProxies/KnownNetworks lægger flere oveni, fx en proxy med en offentlig IP-adresse.
/// </summary>
public static class ForwardedHeadersConfiguration
{
    public const string KnownProxiesKey = "ForwardedHeaders:KnownProxies";
    public const string KnownNetworksKey = "ForwardedHeaders:KnownNetworks";

    /// <summary>Private netværk (RFC 1918 og IPv6 ULA). Loopback (127.0.0.0/8 og ::1) er altid med fra ASP.NET Core.</summary>
    public static readonly IReadOnlyList<System.Net.IPNetwork> PrivateNetworks =
    [
        System.Net.IPNetwork.Parse("10.0.0.0/8"),
        System.Net.IPNetwork.Parse("172.16.0.0/12"),
        System.Net.IPNetwork.Parse("192.168.0.0/16"),
        System.Net.IPNetwork.Parse("fc00::/7")
    ];

    private static readonly char[] Separators = [',', ';', ' '];

    public static void Apply(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        foreach (var network in PrivateNetworks)
        {
            options.KnownIPNetworks.Add(network);
        }

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
                : throw new InvalidOperationException($"{KnownNetworksKey} indeholder \"{value}\", som ikke er et netværk som fx 203.0.113.0/24."));
        }
    }

    private static string[] Split(string? value) =>
        (value ?? string.Empty).Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
