using System.Net;
using web.Repositories.Users.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

public class UsersTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public UsersTests(WebAppFactory factory) => _factory = factory;

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateBrowser();
        await client.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        return client;
    }

    [Fact]
    public async Task Admin_OpretterBruger_SomKanLoggeInd()
    {
        using var admin = await AdminAsync();
        var email = $"ny-{Guid.NewGuid():N}@test.local";
        var response = await admin.PostFormAsync("/Users/Create", "/Users/Create",
            ("UserName", email), ("DisplayName", "Ny Forælder"), ("Password", "En-God-Adgangskode-1"), ("IsAdmin", "false"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        using var user = _factory.CreateBrowser();
        await user.LoginAsync(email, "En-God-Adgangskode-1");
        Assert.Contains("Ny Forælder", await user.GetHtmlAsync("/"));
    }

    [Fact]
    public async Task Opret_MedUgyldigEmailEllerKortAdgangskode_Afvises()
    {
        using var admin = await AdminAsync();
        var response = await admin.PostFormAsync("/Users/Create", "/Users/Create",
            ("UserName", "ikke-en-email"), ("DisplayName", "X"), ("Password", "kort"));
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Skriv en gyldig e-mailadresse.", html);
        Assert.Contains("Adgangskoden skal være mindst 12 tegn.", html);
    }

    [Fact]
    public async Task Opret_MedEksisterendeEmail_Afvises()
    {
        var existing = await _factory.CreateUserAsync($"dublet-{Guid.NewGuid():N}@test.local");
        using var admin = await AdminAsync();
        var response = await admin.PostFormAsync("/Users/Create", "/Users/Create",
            ("UserName", existing.UserName.ToUpperInvariant()), ("DisplayName", "X"), ("Password", "En-God-Adgangskode-1"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Der findes allerede en bruger med denne e-mail.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_KanIkkeSletteSigSelv()
    {
        using var admin = await AdminAsync();
        var adminId = (await _factory.WithServiceAsync<IUserRepository, List<web.Repositories.Users.Dtos.UserDto>>(r => r.GetAllAsync()))
            .Single(u => u.UserName == WebAppFactory.AdminUserName).Id;
        var token = await admin.GetAntiforgeryTokenAsync("/Users");
        var response = await admin.PostAsync($"/Users/Delete/{adminId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_KanIkkeFjerneSinEgenAdminRettighed()
    {
        using var admin = await AdminAsync();
        var adminId = (await _factory.WithServiceAsync<IUserRepository, List<web.Repositories.Users.Dtos.UserDto>>(r => r.GetAllAsync()))
            .Single(u => u.UserName == WebAppFactory.AdminUserName).Id;
        var response = await admin.PostFormAsync($"/Users/Edit/{adminId}", $"/Users/Edit/{adminId}",
            ("DisplayName", "Admin"), ("IsAdmin", "false"), ("IsActive", "true"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Du kan ikke deaktivere dig selv", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SkiftAdgangskode_LoggerAndreSessionerUd()
    {
        var email = $"skift-{Guid.NewGuid():N}@test.local";
        await _factory.CreateUserAsync(email);
        using var first = _factory.CreateBrowser();
        await first.LoginAsync(email, WebAppFactory.UserPassword);
        using var second = _factory.CreateBrowser();
        await second.LoginAsync(email, WebAppFactory.UserPassword);

        var response = await first.PostFormAsync("/Account/ChangePassword", "/Account/ChangePassword",
            ("CurrentPassword", WebAppFactory.UserPassword), ("NewPassword", "Helt-Ny-Adgangskode-9"), ("ConfirmPassword", "Helt-Ny-Adgangskode-9"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        // Den første session er logget ind igen med den nye stamp, den anden er afvist.
        Assert.Equal(HttpStatusCode.OK, (await first.GetAsync("/")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await second.GetAsync("/")).StatusCode);
    }
}
