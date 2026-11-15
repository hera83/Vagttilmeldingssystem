using System.Net;
using web.Tests.Infrastructure;

namespace web.Tests;

public class SmokeTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public SmokeTests(WebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Database_LiggerIDenMidlertidigeMappe()
    {
        using var client = _factory.CreateBrowser();
        await client.GetAsync("/Account/Login");
        Assert.True(File.Exists(_factory.DatabasePath), "app.db skal ligge i testens midlertidige mappe.");
    }

    [Fact]
    public async Task Forside_UdenLogin_SenderTilLogin()
    {
        using var client = _factory.CreateBrowser();
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location?.PathAndQuery);
    }

    [Fact]
    public async Task LoginSide_ViserFormularOgCss()
    {
        using var client = _factory.CreateBrowser();
        var html = await client.GetHtmlAsync("/Account/Login");
        Assert.Contains("Log ind", html);
        Assert.Contains("__RequestVerificationToken", html);

        var css = await client.GetAsync("/css/site.css");
        Assert.Equal(HttpStatusCode.OK, css.StatusCode);
    }

    [Fact]
    public async Task Login_MedFoersteAdministrator_Virker()
    {
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);

        // Administratorens forside er dashboardet (step 13).
        var home = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
        Assert.Equal("/Dashboard", home.Headers.Location?.OriginalString);
        var html = await client.GetHtmlAsync("/Dashboard");
        Assert.Contains("Dashboard", html);
        Assert.Contains("Brugere", html);
    }

    [Fact]
    public async Task Login_MedForkertAdgangskode_GiverFejlbesked()
    {
        using var client = _factory.CreateBrowser();
        var response = await client.PostFormAsync("/Account/Login", "/Account/Login",
            ("UserName", WebAppFactory.AdminUserName), ("Password", "forkert-adgangskode"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Forkert brugernavn eller adgangskode.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_UdenAntiforgeryToken_Afvises()
    {
        using var client = _factory.CreateBrowser();
        var response = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = WebAppFactory.AdminUserName,
            ["Password"] = WebAppFactory.AdminPassword
        }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Bruger_UdenAdmin_FaarIngenAdgangTilBrugere()
    {
        var user = await _factory.CreateUserAsync($"forælder-{Guid.NewGuid():N}@test.local");
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(user.UserName, WebAppFactory.UserPassword);

        var get = await client.GetAsync("/Users");
        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", get.Headers.Location?.PathAndQuery);

        var token = await client.GetAntiforgeryTokenAsync("/Account/ChangePassword");
        var post = await client.PostAsync($"/Users/Delete/{user.Id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", post.Headers.Location?.PathAndQuery);
    }

    [Fact]
    public async Task UkendtSide_ViserStatussiden404()
    {
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        var response = await client.GetAsync("/findes-ikke");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Siden findes ikke", await response.Content.ReadAsStringAsync());
    }
}
