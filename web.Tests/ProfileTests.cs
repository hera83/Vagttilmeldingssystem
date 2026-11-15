using System.Net;
using Microsoft.EntityFrameworkCore;
using web.Repositories.Profiles.Dtos;
using web.Repositories.Profiles.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

public class ProfileTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public ProfileTests(WebAppFactory factory) => _factory = factory;

    public static List<KeyValuePair<string, string>> ValidProfile(string tivoliSheets = "1", string drinkTickets = "4", string shirtSize = "M") =>
    [
        new("FullName", "Mette Hansen"),
        new("Address", "Skolevænget 12, 2. th."),
        new("Phone", "+45 27 34 56 78"),
        new("ShirtSize", shirtSize),
        new("DrinkTickets", drinkTickets),
        new("TivoliSheets", tivoliSheets),
        new("ChildName", "Frederik Hansen"),
        new("ChildClass", "6.A")
    ];

    private async Task<(HttpClient Client, Guid UserId)> NewUserAsync()
    {
        var user = await _factory.CreateUserAsync($"profil-{Guid.NewGuid():N}@test.local");
        var client = _factory.CreateBrowser();
        await client.LoginAsync(user.UserName, WebAppFactory.UserPassword);
        return (client, user.Id);
    }

    [Fact]
    public async Task NyBruger_SerBeskedOmAtUdfyldeProfilen()
    {
        var (client, _) = await NewUserAsync();
        Assert.Contains("Udfyld din profil.", await client.GetHtmlAsync("/"));
        Assert.Contains("Udfyld alle felter med *.", await client.GetHtmlAsync("/Profile"));
    }

    [Fact]
    public async Task GyldigProfil_GemmesOgErKomplet()
    {
        var (client, userId) = await NewUserAsync();
        var response = await client.PostFormAsync("/Profile", "/Profile", ValidProfile());
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var profile = await _factory.WithServiceAsync<IProfileRepository, ProfileDto?>(r => r.GetByUserIdAsync(userId));
        Assert.NotNull(profile);
        Assert.True(profile.IsComplete);
        Assert.Equal("Skolevænget 12, 2. th.", profile.Address);
        Assert.DoesNotContain("Udfyld din profil.", await client.GetHtmlAsync("/"));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    public async Task Tivoliark_0Til2_Accepteres(string sheets)
    {
        var (client, userId) = await NewUserAsync();
        var response = await client.PostFormAsync("/Profile", "/Profile", ValidProfile(tivoliSheets: sheets));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var profile = await _factory.WithServiceAsync<IProfileRepository, ProfileDto?>(r => r.GetByUserIdAsync(userId));
        Assert.Equal(int.Parse(sheets), profile!.TivoliSheets);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("-1")]
    public async Task Tivoliark_UdenforGraensen_Afvises(string sheets)
    {
        var (client, userId) = await NewUserAsync();
        var response = await client.PostFormAsync("/Profile", "/Profile", ValidProfile(tivoliSheets: sheets));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Du kan højst få 2 ark tivolibilletter.", await response.Content.ReadAsStringAsync());
        Assert.Null(await _factory.WithServiceAsync<IProfileRepository, ProfileDto?>(r => r.GetByUserIdAsync(userId)));
    }

    [Fact]
    public async Task Tivoliark3_AfvisesOgsaaAfDatabasen()
    {
        var user = await _factory.CreateUserAsync($"db-{Guid.NewGuid():N}@test.local");
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _factory.WithServiceAsync<IProfileRepository, ProfileDto>(r => r.SaveAsync(user.Id, new SaveProfileDto
            {
                FullName = "X", Address = "X", Phone = "12345678", ShirtSize = "M", ChildName = "X", ChildClass = "1.A",
                TivoliSheets = 3
            })));
    }

    [Fact]
    public async Task TekstITalfelt_GiverDanskFejl()
    {
        var (client, _) = await NewUserAsync();
        var response = await client.PostFormAsync("/Profile", "/Profile", ValidProfile(drinkTickets: "abc"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Værdien »abc« er ikke gyldig for Væskebilletter.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UkendtTroejestoerrelse_Afvises()
    {
        var (client, userId) = await NewUserAsync();
        var response = await client.PostFormAsync("/Profile", "/Profile", ValidProfile(shirtSize: "XXXXL"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Vælg en trøjestørrelse fra listen.", await response.Content.ReadAsStringAsync());
        Assert.Null(await _factory.WithServiceAsync<IProfileRepository, ProfileDto?>(r => r.GetByUserIdAsync(userId)));
    }

    [Fact]
    public async Task IDOR_EtUserIdIFormularen_GemmerAldrigPaaEnAndenBruger()
    {
        var victim = await _factory.CreateUserAsync($"offer-{Guid.NewGuid():N}@test.local");
        var (attacker, attackerId) = await NewUserAsync();

        var fields = ValidProfile();
        fields.Add(new("UserId", victim.Id.ToString()));
        await attacker.PostFormAsync("/Profile", "/Profile", fields);

        Assert.Null(await _factory.WithServiceAsync<IProfileRepository, ProfileDto?>(r => r.GetByUserIdAsync(victim.Id)));
        Assert.NotNull(await _factory.WithServiceAsync<IProfileRepository, ProfileDto?>(r => r.GetByUserIdAsync(attackerId)));
    }

    [Fact]
    public async Task Bruger_KanIkkeSeAndresProfilViaAdminSiden()
    {
        var other = await _factory.CreateUserAsync($"anden-{Guid.NewGuid():N}@test.local");
        var (client, _) = await NewUserAsync();
        var response = await client.GetAsync($"/Users/Profile/{other.Id}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", response.Headers.Location?.PathAndQuery);
    }

    [Fact]
    public async Task Admin_SerProfilstatusOgProfil()
    {
        var (client, userId) = await NewUserAsync();
        using var admin = _factory.CreateBrowser();
        await admin.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);

        Assert.Contains("Mangler", await admin.GetHtmlAsync("/Users"));
        Assert.Contains("ikke udfyldt sin profil", await admin.GetHtmlAsync($"/Users/Profile/{userId}"));

        await client.PostFormAsync("/Profile", "/Profile", ValidProfile(tivoliSheets: "2"));
        var html = await admin.GetHtmlAsync($"/Users/Profile/{userId}");
        Assert.Contains("Skolevænget 12, 2. th.", html);
        Assert.Contains("2 ark", html);
        Assert.Contains("Udfyldt", await admin.GetHtmlAsync("/Users"));
    }

    [Fact]
    public async Task Admin_RetterEmail_BrugerenLoggesUdOgBrugerDenNye()
    {
        var oldEmail = $"gammel-{Guid.NewGuid():N}@test.local";
        var newEmail = $"ny-{Guid.NewGuid():N}@test.local";
        var user = await _factory.CreateUserAsync(oldEmail, displayName: "Skift Mail");
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(oldEmail, WebAppFactory.UserPassword);

        using var admin = _factory.CreateBrowser();
        await admin.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        var response = await admin.PostFormAsync($"/Users/Edit/{user.Id}", $"/Users/Edit/{user.Id}",
            ("UserName", newEmail), ("DisplayName", "Skift Mail"), ("IsAdmin", "false"), ("IsActive", "true"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/")).StatusCode);
        using var fresh = _factory.CreateBrowser();
        await fresh.LoginAsync(newEmail, WebAppFactory.UserPassword);
        Assert.Contains(newEmail, await fresh.GetHtmlAsync("/Profile"));
    }

    [Fact]
    public async Task Admin_RetterEmailTilEnEksisterende_Afvises()
    {
        var taken = await _factory.CreateUserAsync($"taget-{Guid.NewGuid():N}@test.local");
        var user = await _factory.CreateUserAsync($"ret-{Guid.NewGuid():N}@test.local");
        using var admin = _factory.CreateBrowser();
        await admin.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        var response = await admin.PostFormAsync($"/Users/Edit/{user.Id}", $"/Users/Edit/{user.Id}",
            ("UserName", taken.UserName), ("DisplayName", "X"), ("IsAdmin", "false"), ("IsActive", "true"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Der findes allerede en bruger med denne e-mail.", await response.Content.ReadAsStringAsync());
    }
}
