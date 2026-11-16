using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using web.Data;
using web.Data.Models;
using web.Managers.Enrollment.Dtos;
using web.Managers.Enrollment.Interfaces;
using web.Repositories.Users.Dtos;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>Ledige vagter og samlet tilmelding (krav K02, K10, K13, K14, K15).</summary>
public class SignupTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public SignupTests(WebAppFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, UserDto User)> ParentAsync(bool completeProfile = true)
    {
        var user = await _factory.CreateUserAsync($"forælder-{Guid.NewGuid():N}@test.local", displayName: "Forælder");
        if (completeProfile)
        {
            await _factory.CompleteProfileAsync(user.Id);
        }

        var client = _factory.CreateBrowser();
        await client.LoginAsync(user.UserName, WebAppFactory.UserPassword);
        return (client, user);
    }

    private static Task<HttpResponseMessage> EnrollAsync(HttpClient client, params Guid[] shiftIds) =>
        client.PostFormAsync("/Signup", "/Signup/Enroll", shiftIds.Select(id => new KeyValuePair<string, string>("ShiftIds", id.ToString())));

    private async Task<int> ActiveCountAsync(Guid? shiftId = null, Guid? userId = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WebDbContext>();
        return await db.Assignments.CountAsync(a => a.IsActive && (shiftId == null || a.ShiftId == shiftId) && (userId == null || a.UserId == userId));
    }

    private async Task<(Guid EventId, List<Guid> Shifts)> OpenEventAsync(int count, int capacity = 4, string prefix = "Vagt")
    {
        var ev = await _factory.CreateEventAsync();
        var ids = new List<Guid>();
        for (var i = 0; i < count; i++)
        {
            ids.Add((await _factory.CreateShiftAsync(ev.Id, capacity, title: $"{prefix} {i + 1} {Guid.NewGuid():N}"[..20])).Id);
        }

        await _factory.OpenShiftsAsync([.. ids]);
        return (ev.Id, ids);
    }

    [Fact]
    public async Task Begivenhedskort_ViserPeriodenForDeAabneVagter()
    {
        // Fundet i step 17: kortet viste kun datoen for den første vagt, selvom begivenheden varede flere dage.
        var ev = await _factory.CreateEventAsync(new DateOnly(2027, 4, 16));
        var first = await _factory.CreateShiftAsync(ev.Id, 2, new DateTime(2027, 4, 16, 8, 0, 0, DateTimeKind.Utc), "Periode første");
        var last = await _factory.CreateShiftAsync(ev.Id, 2, new DateTime(2027, 4, 18, 8, 0, 0, DateTimeKind.Utc), "Periode sidste");
        await _factory.OpenShiftsAsync(first.Id, last.Id);
        var (client, _) = await ParentAsync();

        var html = await client.GetHtmlAsync($"/Signup?eventId={ev.Id}");

        Assert.Contains("fre. 16. apr. – søn. 18. apr. 2027", html);
    }

    [Fact]
    public async Task KunAabneVagter_Vises_AldrigKladderEllerArkiverede()
    {
        var ev = await _factory.CreateEventAsync();
        var open = await _factory.CreateShiftAsync(ev.Id, 3, title: "Synlig kiosk");
        await _factory.CreateShiftAsync(ev.Id, 3, title: "Hemmelig kladde");
        await _factory.OpenShiftsAsync(open.Id);
        var archived = await _factory.CreateEventAsync(archived: true);
        var archivedShift = await _factory.CreateShiftAsync(archived.Id, 3, title: "Arkiveret bod");
        await _factory.OpenShiftsAsync(archivedShift.Id);
        var (client, _) = await ParentAsync();

        var html = await client.GetHtmlAsync("/Signup");
        Assert.Contains("Synlig kiosk", html);
        Assert.DoesNotContain("Hemmelig kladde", html);
        Assert.DoesNotContain("Arkiveret bod", html);
    }

    [Fact]
    public async Task TreVagter_TilmeldesSamlet_OgVisesUnderMineVagter()
    {
        var (_, shifts) = await OpenEventAsync(3);
        var (client, user) = await ParentAsync();

        var response = await EnrollAsync(client, [.. shifts]);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/MyShifts", response.Headers.Location?.OriginalString);
        Assert.Equal(3, await ActiveCountAsync(userId: user.Id));

        var mine = await client.GetHtmlAsync("/MyShifts");
        Assert.Contains("Du er tilmeldt 3 vagter.", mine);
        Assert.Contains("Du er på", await client.GetHtmlAsync("/Signup"));
    }

    [Fact]
    public async Task EnFuldVagtIValget_GiverIngenTilmeldinger_OgForstaaeligBesked()
    {
        var (_, shifts) = await OpenEventAsync(3, capacity: 1);
        var (other, _) = await ParentAsync();
        await EnrollAsync(other, shifts[1]);
        var (client, user) = await ParentAsync();

        var response = await EnrollAsync(client, [.. shifts]);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Du er ikke tilmeldt nogen af vagterne", html);
        Assert.Contains("blev fuld, før din tilmelding blev gennemført.", html);
        Assert.Equal(0, await ActiveCountAsync(userId: user.Id));
    }

    [Fact]
    public async Task ProfilMangler_ServerAfviser_OgKnappenErSlaaetFra()
    {
        var (_, shifts) = await OpenEventAsync(1);
        var (client, user) = await ParentAsync(completeProfile: false);

        Assert.Contains("Udfyld din profil først.", await client.GetHtmlAsync("/Signup"));
        var response = await EnrollAsync(client, shifts[0]);
        Assert.Contains("Du skal udfylde din profil, før du kan tilmelde dig vagter.", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, await ActiveCountAsync(userId: user.Id));
    }

    [Fact]
    public async Task KladdeIdIPost_AfvisesUdenAtAfsloereKladden()
    {
        var ev = await _factory.CreateEventAsync();
        var draft = await _factory.CreateShiftAsync(ev.Id, 3, title: "Topsecret bod");
        var (client, user) = await ParentAsync();

        var response = await EnrollAsync(client, draft.Id);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("En af de valgte vagter er ikke åben for tilmelding.", html);
        Assert.DoesNotContain("Topsecret", html);
        Assert.Equal(0, await ActiveCountAsync(userId: user.Id));
    }

    [Fact]
    public async Task IngenValgt_GiverBesked()
    {
        await OpenEventAsync(1);
        var (client, _) = await ParentAsync();
        var response = await client.PostFormAsync("/Signup", "/Signup/Enroll");
        Assert.Contains("Vælg mindst én vagt, før du bekræfter.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UdenAntiforgeryToken_Afvises()
    {
        var (_, shifts) = await OpenEventAsync(1);
        var (client, user) = await ParentAsync();
        var response = await client.PostAsync("/Signup/Enroll", new FormUrlEncodedContent(new Dictionary<string, string> { ["ShiftIds"] = shifts[0].ToString() }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await ActiveCountAsync(userId: user.Id));
    }

    [Fact]
    public async Task FemtiSamtidigeViaManageren_GiverEnPlads()
    {
        var (_, shifts) = await OpenEventAsync(1, capacity: 1);
        var users = await _factory.CreateBulkUsersAsync(50);
        foreach (var id in users)
        {
            await _factory.CompleteProfileAsync(id);
        }

        var results = await Task.WhenAll(users.Select(u => Task.Run(async () =>
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<IEnrollmentManager>().EnrollAsync(u, [shifts[0]]);
        })));

        Assert.Single(results, r => r.Status == EnrollStatus.Success);
        Assert.Equal(49, results.Count(r => r.Status == EnrollStatus.Conflict));
        Assert.Equal(1, await ActiveCountAsync(shiftId: shifts[0]));
    }

    [Fact]
    public async Task TiSamtidigeHttpRequestsOmSidstePlads_GiverEnTilmelding()
    {
        var (_, shifts) = await OpenEventAsync(1, capacity: 1);
        var parents = new List<HttpClient>();
        for (var i = 0; i < 10; i++)
        {
            parents.Add((await ParentAsync()).Client);
        }

        // Hent alle tokens først, så POST'erne rammer serveren så samtidigt som muligt.
        var tokens = await Task.WhenAll(parents.Select(p => p.GetAntiforgeryTokenAsync("/Signup")));
        var responses = await Task.WhenAll(parents.Select((p, i) => p.PostAsync("/Signup/Enroll", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["ShiftIds"] = shifts[0].ToString(), ["__RequestVerificationToken"] = tokens[i]
        }))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Redirect);
        Assert.Equal(9, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, await ActiveCountAsync(shiftId: shifts[0]));
        parents.ForEach(p => p.Dispose());
    }

    [Fact]
    public async Task Forsiden_ViserNaesteVagtEfterTilmelding()
    {
        var (_, shifts) = await OpenEventAsync(1, prefix: "Forsidevagt");
        var (client, _) = await ParentAsync();
        Assert.Contains("Du har ingen kommende vagter.", await client.GetHtmlAsync("/"));

        await EnrollAsync(client, shifts[0]);
        var html = await client.GetHtmlAsync("/");
        Assert.Contains("Forsidevagt", html);
        Assert.Contains("Du har 1 kommende vagt.", html);
    }

    [Fact]
    public async Task SelvTilmeldt_HarKildenSelf()
    {
        var (_, shifts) = await OpenEventAsync(1);
        var (client, user) = await ParentAsync();
        await EnrollAsync(client, shifts[0]);

        await using var scope = _factory.Services.CreateAsyncScope();
        var row = await scope.ServiceProvider.GetRequiredService<WebDbContext>().Assignments.SingleAsync(a => a.UserId == user.Id);
        Assert.Equal(AssignmentSource.Self, row.Source);
        Assert.Null(row.AssignedByUserId);
    }
}
