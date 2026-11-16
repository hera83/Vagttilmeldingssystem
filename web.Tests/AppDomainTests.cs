using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using web.Infrastructure.Notifications;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>
/// App:Domain (step 19): kun domænet skrives. Links i mails er https i drift og http i udvikling, og systemet svarer kun
/// på domænet og localhost.
/// </summary>
public class AppDomainTests : IClassFixture<ProxyWebAppFactory>
{
    private readonly ProxyWebAppFactory _factory;

    public AppDomainTests(ProxyWebAppFactory factory) => _factory = factory;

    private static AppLinks Links(string environmentName, params (string Key, string Value)[] settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(x => new KeyValuePair<string, string?>(x.Key, x.Value)))
            .Build();
        return new AppLinks(configuration, new HostingEnvironment { EnvironmentName = environmentName });
    }

    [Theory]
    [InlineData("vagter.skolen.dk", "vagter.skolen.dk")]
    [InlineData("  Vagter.Skolen.DK  ", "vagter.skolen.dk")]
    [InlineData("https://vagter.skolen.dk/", "vagter.skolen.dk")]
    [InlineData("http://vagter.skolen.dk", "vagter.skolen.dk")]
    [InlineData("vagter.skolen.dk:443", "vagter.skolen.dk")]
    [InlineData("vagter.skolen.dk:8443", "vagter.skolen.dk:8443")]
    [InlineData("localhost:5180", "localhost:5180")]
    public void ParseDomain_AccepterarDomaenetOgRydderOp(string value, string expected) =>
        Assert.Equal(expected, AppLinks.ParseDomain(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://")]
    public void ParseDomain_TomVaerdi_ErIkkeSat(string? value) =>
        Assert.Null(AppLinks.ParseDomain(value));

    [Theory]
    [InlineData("vagter.skolen.dk/vagter")]
    [InlineData("vagter.skolen.dk?x=1")]
    [InlineData("bruger@vagter.skolen.dk")]
    [InlineData("vagter skolen dk")]
    public void ParseDomain_UgyldigVaerdi_NaevnerNoeglen(string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => AppLinks.ParseDomain(value));
        Assert.Contains(AppLinks.DomainKey, error.Message);
    }

    [Fact]
    public void IDrift_ErLinksHttps()
    {
        var links = Links(Environments.Production, (AppLinks.DomainKey, "vagter.skolen.dk"));

        Assert.Equal("https://vagter.skolen.dk", links.BaseUrl);
        Assert.Equal("https://vagter.skolen.dk/MyShifts", links.Absolute("/MyShifts"));
    }

    [Fact]
    public void IDrift_UdenDomaene_ErIkkeSatOp_SelvOmAppenLytterPaaEnAdresse()
    {
        var links = Links(Environments.Production, ("urls", "http://+:8080"));

        Assert.Null(links.BaseUrl);
        Assert.False(links.IsConfigured);
        var error = Assert.Throws<InvalidOperationException>(() => links.Absolute("/Account/SetPassword"));
        Assert.Contains(AppLinks.DomainKey, error.Message);
    }

    [Fact]
    public void IUdvikling_ErLinksHttp()
    {
        var links = Links(Environments.Development, (AppLinks.DomainKey, "vagter.test"));

        Assert.Equal("http://vagter.test", links.BaseUrl);
    }

    [Theory]
    [InlineData("https://localhost:7180;http://localhost:5180", "http://localhost:5180")]
    [InlineData("http://localhost:5180", "http://localhost:5180")]
    [InlineData("http://0.0.0.0:5180", "http://localhost:5180")]
    [InlineData("http://+:5180/", "http://localhost:5180")]
    [InlineData("https://localhost:7180", "https://localhost:7180")]
    public void IUdvikling_UdenDomaene_BrugesAppensEgenAdresse(string urls, string expected)
    {
        var links = Links(Environments.Development, ("urls", urls));

        Assert.Equal(expected, links.BaseUrl);
    }

    private static Task<HttpContext> GetLoginAsync(WebAppFactory factory, string host) =>
        factory.Server.SendAsync(context =>
        {
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = "/Account/Login";
            context.Request.Host = new HostString(host);
            // I drift kræver anti-forgery https, så requesten kommer som fra proxyen.
            context.Request.Scheme = "https";
            context.Connection.RemoteIpAddress = IPAddress.Loopback;
        });

    [Theory]
    [InlineData(WebAppFactory.Domain, StatusCodes.Status200OK)]
    [InlineData("VAGTER.TEST", StatusCodes.Status200OK)]
    [InlineData("localhost", StatusCodes.Status200OK)]
    [InlineData("localhost:8080", StatusCodes.Status200OK)]
    [InlineData("angriber.test", StatusCodes.Status400BadRequest)]
    [InlineData("vagter.test.angriber.test", StatusCodes.Status400BadRequest)]
    public async Task SvarerKunPaaDomaenetOgLocalhost(string host, int expectedStatus)
    {
        var context = await GetLoginAsync(_factory, host);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
    }

    [Fact]
    public async Task AllowedHostsSatIHaanden_Vinder()
    {
        using var factory = new AllowedHostsWebAppFactory();

        Assert.Equal(StatusCodes.Status200OK, (await GetLoginAsync(factory, "andet.test")).Response.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, (await GetLoginAsync(factory, WebAppFactory.Domain)).Response.StatusCode);
    }

    [Fact]
    public void UgyldigtDomaene_StopperOpstarten()
    {
        using var factory = new InvalidDomainWebAppFactory();

        var error = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(error);
        Assert.Contains(AppLinks.DomainKey, error.ToString());
    }

    private sealed class AllowedHostsWebAppFactory : WebAppFactory
    {
        protected override void ConfigureTestServices(IWebHostBuilder builder) =>
            builder.UseSetting("AllowedHosts", "andet.test");
    }

    private sealed class InvalidDomainWebAppFactory : WebAppFactory
    {
        protected override void ConfigureTestServices(IWebHostBuilder builder) =>
            builder.UseSetting(AppLinks.DomainKey, "vagter.test/sti");
    }
}
