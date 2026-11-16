using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using web.Infrastructure.Security;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>
/// Drift bag en reverse proxy og i en container (step 16 og 19): X-Forwarded-* og data protection-nøgler.
/// Private netværk og samme maskine er betroet uden konfiguration. Alle andre afsendere ignoreres, medmindre de står i ForwardedHeaders:*.
/// </summary>
public class DockerReadinessTests : IClassFixture<ProxyWebAppFactory>
{
    private const string ClientIp = "198.51.100.7";
    private const string UnknownPublicSender = "192.0.2.9";

    private readonly ProxyWebAppFactory _factory;

    public DockerReadinessTests(ProxyWebAppFactory factory) => _factory = factory;

    private static Task<HttpContext> SendAsync(WebAppFactory factory, string remoteIp) =>
        factory.Server.SendAsync(context =>
        {
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/Account/Login";
            context.Request.Host = new HostString(WebAppFactory.Domain);
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            context.Request.Headers["X-Forwarded-For"] = ClientIp;
            context.Request.Headers["X-Forwarded-Proto"] = "https";
        });

    [Theory]
    [InlineData("10.0.0.5")]                       // privat netværk
    [InlineData("172.18.0.3")]                     // Dockers netværk (Caddy i compose, nginx på værten)
    [InlineData("192.168.1.20")]                   // proxy på LAN (fx NAS eller router)
    [InlineData("::ffff:172.18.0.3")]              // IPv4 som IPv6, som Kestrel ser det, når den lytter på [::]
    [InlineData("fd00::5")]                        // privat IPv6 (ULA)
    [InlineData(ProxyWebAppFactory.ConfiguredProxy)]
    [InlineData("100.64.1.20")]                    // i ForwardedHeaders:KnownNetworks
    public async Task FraBetroetAfsender_SesKlientensIpOgHttps(string proxyIp)
    {
        var context = await SendAsync(_factory, proxyIp);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("https", context.Request.Scheme);
        Assert.Equal(IPAddress.Parse(ClientIp), context.Connection.RemoteIpAddress);
        Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));

        // Over https (her via proxyen) er anti-forgery-cookien (og alle andre) Secure, ligesom login-cookien.
        Assert.True(context.Response.Headers.SetCookie.Count > 0);
        Assert.All(context.Response.Headers.SetCookie, cookie => Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FraUkendtOffentligAfsender_IgnoreresHeaderne()
    {
        var context = await SendAsync(_factory, UnknownPublicSender);

        Assert.Equal("http", context.Request.Scheme);
        Assert.Equal(IPAddress.Parse(UnknownPublicSender), context.Connection.RemoteIpAddress);
        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));

        // Requesten behandles som http (step 20): login-siden vises, og cookies er uden Secure.
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.True(context.Response.Headers.SetCookie.Count > 0);
        Assert.All(context.Response.Headers.SetCookie, cookie => Assert.DoesNotContain("; secure", cookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UdenKonfiguration_StolerPaaSammeMaskineOgPrivateNetvaerk()
    {
        using var factory = new WebAppFactory();

        var local = await SendAsync(factory, "127.0.0.1");
        var docker = await SendAsync(factory, "172.18.0.3");
        var publicProxy = await SendAsync(factory, ProxyWebAppFactory.ConfiguredProxy);

        Assert.Equal("https", local.Request.Scheme);
        Assert.Equal("https", docker.Request.Scheme);
        Assert.Equal(IPAddress.Parse(ClientIp), docker.Connection.RemoteIpAddress);
        Assert.Equal("http", publicProxy.Request.Scheme);
        Assert.Equal(IPAddress.Parse(ProxyWebAppFactory.ConfiguredProxy), publicProxy.Connection.RemoteIpAddress);
    }

    [Theory]
    [InlineData(ForwardedHeadersConfiguration.KnownProxiesKey, "proxy.local")]
    [InlineData(ForwardedHeadersConfiguration.KnownNetworksKey, "172.16.0.0")]
    public void UgyldigKonfiguration_NaevnerNoeglen(string key, string value)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection([new(key, value)]).Build();

        var error = Assert.Throws<InvalidOperationException>(() => ForwardedHeadersConfiguration.Apply(new ForwardedHeadersOptions(), configuration));

        Assert.Contains(key, error.Message);
    }

    [Fact]
    public async Task DataProtectionNoegler_GemmesIAppDbs()
    {
        await SendAsync(_factory, "172.18.0.3");

        var keys = Path.Combine(_factory.RootPath, "app_dbs", "keys");
        Assert.NotEmpty(Directory.GetFiles(keys, "key-*.xml"));
    }
}
