using System.Net;
using System.Text.Json;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>PWA (krav K01): manifest, service worker, offline-side og installationsvejledning skal kunne hentes uden login.</summary>
public class PwaTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public PwaTests(WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Manifest_UdenLogin_ErGyldigtOgIkonerneFindes()
    {
        using var client = _factory.CreateBrowser();
        var response = await client.GetAsync("/manifest.webmanifest");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/manifest+json", response.Content.Headers.ContentType?.MediaType);

        using var manifest = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = manifest.RootElement;
        Assert.Equal("Vagttilmelding", root.GetProperty("name").GetString());
        Assert.Equal("standalone", root.GetProperty("display").GetString());
        Assert.Equal("/", root.GetProperty("start_url").GetString());
        Assert.Equal("da", root.GetProperty("lang").GetString());

        var icons = root.GetProperty("icons").EnumerateArray().ToList();
        Assert.Contains(icons, icon => icon.GetProperty("sizes").GetString() == "192x192");
        Assert.Contains(icons, icon => icon.GetProperty("sizes").GetString() == "512x512" && icon.GetProperty("purpose").GetString() == "any");
        Assert.Contains(icons, icon => icon.GetProperty("purpose").GetString() == "maskable");
        foreach (var icon in icons)
        {
            var iconResponse = await client.GetAsync(icon.GetProperty("src").GetString());
            Assert.Equal(HttpStatusCode.OK, iconResponse.StatusCode);
            Assert.Equal(icon.GetProperty("type").GetString(), iconResponse.Content.Headers.ContentType?.MediaType);
        }
    }

    [Fact]
    public async Task ServiceWorker_UdenLogin_LiggerPaaRodenOgCacherIkkeSider()
    {
        using var client = _factory.CreateBrowser();
        var response = await client.GetAsync("/service-worker.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType);
        var script = await response.Content.ReadAsStringAsync();
        Assert.Contains("/offline.html", script);
        Assert.Contains("request.mode === 'navigate'", script);
    }

    [Fact]
    public async Task OfflineSide_UdenLogin_KanHentes()
    {
        using var client = _factory.CreateBrowser();
        var response = await client.GetAsync("/offline.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Du er offline", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AppleTouchIcon_UdenLogin_KanHentes()
    {
        using var client = _factory.CreateBrowser();
        var response = await client.GetAsync("/icons/apple-touch-icon.png");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Installationsvejledning_UdenLogin_ViserAlleTrePlatforme()
    {
        using var client = _factory.CreateBrowser();
        var html = await client.GetHtmlAsync("/Home/Install");

        Assert.Contains("Installér appen", html);
        Assert.Contains("data-platform=\"android\"", html);
        Assert.Contains("data-platform=\"ios\"", html);
        Assert.Contains("data-platform=\"desktop\"", html);
        Assert.Contains("Føj til hjemmeskærm", html);
    }

    [Fact]
    public async Task LoginSide_HarManifestOgInstallationssektion()
    {
        using var client = _factory.CreateBrowser();
        var html = await client.GetHtmlAsync("/Account/Login");

        Assert.Contains("rel=\"manifest\" href=\"/manifest.webmanifest\"", html);
        Assert.Contains("apple-touch-icon", html);
        Assert.Contains("Installér på din enhed", html);
        Assert.Contains("href=\"/Home/Install\"", html);
        Assert.Contains("/js/pwa.js", html);
        Assert.Contains("data-offline-status", html);
    }

    [Fact]
    public async Task Forside_IndloggetBruger_HarOfflineBjaelkeOgInstallationsLink()
    {
        var user = await _factory.CreateUserAsync($"pwa-{Guid.NewGuid():N}@test.local");
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(user.UserName, WebAppFactory.UserPassword);

        var html = await client.GetHtmlAsync("/");

        Assert.Contains("data-offline-status", html);
        Assert.Contains("href=\"/Home/Install\"", html);
    }
}
