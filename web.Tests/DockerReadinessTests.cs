using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using web.Infrastructure.Security;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>Drift bag en reverse proxy og i en container (step 16): X-Forwarded-* og data protection-nøgler.</summary>
public class DockerReadinessTests : IClassFixture<ProxyWebAppFactory>
{
    private const string ClientIp = "203.0.113.7";

    private readonly ProxyWebAppFactory _factory;

    public DockerReadinessTests(ProxyWebAppFactory factory) => _factory = factory;

    private Task<HttpContext> SendAsync(WebAppFactory factory, string remoteIp) =>
        factory.Server.SendAsync(context =>
        {
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/Account/Login";
            context.Request.Host = new HostString("vagter.test");
            context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            context.Request.Headers["X-Forwarded-For"] = ClientIp;
            context.Request.Headers["X-Forwarded-Proto"] = "https";
        });

    [Theory]
    [InlineData(ProxyWebAppFactory.KnownProxy)]
    [InlineData("192.168.50.20")]
    public async Task FraKendtProxy_SesKlientensIpOgHttps(string proxyIp)
    {
        var context = await SendAsync(_factory, proxyIp);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("https", context.Request.Scheme);
        Assert.Equal(IPAddress.Parse(ClientIp), context.Connection.RemoteIpAddress);
        Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));

        // Anti-forgery-cookien (og alle andre) er Secure i drift, ligesom login-cookien.
        Assert.NotEmpty(context.Response.Headers.SetCookie);
        Assert.All(context.Response.Headers.SetCookie, cookie => Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FraUkendtAfsender_IgnoreresHeaderne()
    {
        var context = await SendAsync(_factory, "198.51.100.9");

        Assert.Equal("http", context.Request.Scheme);
        Assert.Equal(IPAddress.Parse("198.51.100.9"), context.Connection.RemoteIpAddress);
        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task UdenKonfiguration_StolerKunPaaSammeMaskine()
    {
        using var factory = new WebAppFactory();

        var remote = await SendAsync(factory, ProxyWebAppFactory.KnownProxy);
        var local = await SendAsync(factory, "127.0.0.1");

        Assert.Equal("http", remote.Request.Scheme);
        Assert.Equal("https", local.Request.Scheme);
        Assert.Equal(IPAddress.Parse(ClientIp), local.Connection.RemoteIpAddress);
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
        await SendAsync(_factory, ProxyWebAppFactory.KnownProxy);

        var keys = Path.Combine(_factory.RootPath, "app_dbs", "keys");
        Assert.NotEmpty(Directory.GetFiles(keys, "key-*.xml"));
    }
}
