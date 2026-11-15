using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using web.Data;
using web.Repositories.Users.Dtos;
using web.Repositories.Users.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

public class AssignmentsTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public AssignmentsTests(WebAppFactory factory) => _factory = factory;

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateBrowser();
        await client.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        return client;
    }

    private async Task<(HttpClient Client, UserDto User)> ParentAsync(string name)
    {
        var user = await _factory.CreateUserAsync($"{name}-{Guid.NewGuid():N}@test.local", displayName: name);
        var client = _factory.CreateBrowser();
        await client.LoginAsync(user.UserName, WebAppFactory.UserPassword);
        return (client, user);
    }

    private static Task<HttpResponseMessage> AssignAsync(HttpClient admin, Guid shiftId, Guid userId) =>
        admin.PostFormAsync($"/Shifts/Assign/{shiftId}", $"/Shifts/Assign/{shiftId}", ("userId", userId.ToString()));

    private async Task<Guid> ActiveAssignmentIdAsync(Guid shiftId, Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WebDbContext>();
        return await db.Assignments.Where(a => a.ShiftId == shiftId && a.UserId == userId && a.IsActive).Select(a => a.Id).SingleAsync();
    }

    [Fact]
    public async Task SkjultTildeling_SesKunAfModtageren()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 4, title: "Hemmelig kiosk");
        var (a, userA) = await ParentAsync("Anna");
        var (b, _) = await ParentAsync("Bent");
        using var admin = await AdminAsync();

        Assert.Equal(HttpStatusCode.Redirect, (await AssignAsync(admin, shift.Id, userA.Id)).StatusCode);

        var aList = await a.GetHtmlAsync("/MyShifts");
        Assert.Contains("Hemmelig kiosk", aList);
        Assert.Contains("Tildelt af administrator", aList);
        Assert.Equal(HttpStatusCode.OK, (await a.GetAsync($"/MyShifts/Details/{shift.Id}")).StatusCode);

        Assert.DoesNotContain("Hemmelig kiosk", await b.GetHtmlAsync("/MyShifts"));
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/MyShifts/Details/{shift.Id}")).StatusCode);
    }

    [Fact]
    public async Task AdminFjerner_BrugerenSerIkkeVagtenMere_OgHistorikkenBevares()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 4, title: "Oprydning");
        var (a, userA) = await ParentAsync("Anna");
        using var admin = await AdminAsync();
        await AssignAsync(admin, shift.Id, userA.Id);
        var assignmentId = await ActiveAssignmentIdAsync(shift.Id, userA.Id);

        var response = await admin.PostFormAsync($"/Shifts/Assign/{shift.Id}", $"/Shifts/Unassign/{shift.Id}", ("assignmentId", assignmentId.ToString()));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        Assert.DoesNotContain("Oprydning", await a.GetHtmlAsync("/MyShifts"));
        await using var scope = _factory.Services.CreateAsyncScope();
        var row = await scope.ServiceProvider.GetRequiredService<WebDbContext>().Assignments.SingleAsync(x => x.Id == assignmentId);
        Assert.False(row.IsActive);
        Assert.NotNull(row.CancelledAt);
    }

    [Fact]
    public async Task FuldVagt_KanIkkeOverbookesAfAdmin()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 1);
        var (_, first) = await ParentAsync("Først");
        var (_, second) = await ParentAsync("Anden");
        using var admin = await AdminAsync();

        await AssignAsync(admin, shift.Id, first.Id);
        await AssignAsync(admin, shift.Id, second.Id);

        Assert.Contains("Vagten er fuld.", await admin.GetHtmlAsync($"/Shifts/Assign/{shift.Id}"));
        Assert.Contains("1 af 1", await admin.GetHtmlAsync($"/Events/Details/{ev.Id}"));
    }

    [Fact]
    public async Task Kapacitet_KanIkkeSaenkesUnderAntalPaaVagten()
    {
        var ev = await _factory.CreateEventAsync(new DateOnly(2027, 6, 1));
        var shift = await _factory.CreateShiftAsync(ev.Id, 3, new DateTime(2027, 6, 1, 15, 0, 0, DateTimeKind.Utc));
        var users = await _factory.CreateBulkUsersAsync(2);
        using var admin = await AdminAsync();
        await AssignAsync(admin, shift.Id, users[0]);
        await AssignAsync(admin, shift.Id, users[1]);

        var response = await admin.PostFormAsync($"/Shifts/Edit/{shift.Id}", $"/Shifts/Edit/{shift.Id}",
            ("Title", "Kiosk"), ("Date", "2027-06-01"), ("StartTime", "17:00"), ("EndTime", "19:00"), ("Capacity", "1"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Der er allerede flere personer på vagten end det nye antal pladser.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task VagtMedPersoner_KanIkkeSlettes_FoerDeErFjernet()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2, title: "Garderobe");
        var (_, user) = await ParentAsync("Anna");
        using var admin = await AdminAsync();
        await AssignAsync(admin, shift.Id, user.Id);

        await admin.PostFormAsync($"/Events/Details/{ev.Id}", $"/Shifts/Delete/{shift.Id}");
        Assert.Contains("har personer på og kan ikke slettes", await admin.GetHtmlAsync($"/Events/Details/{ev.Id}"));

        var assignmentId = await ActiveAssignmentIdAsync(shift.Id, user.Id);
        await admin.PostFormAsync($"/Shifts/Assign/{shift.Id}", $"/Shifts/Unassign/{shift.Id}", ("assignmentId", assignmentId.ToString()));
        await admin.PostFormAsync($"/Events/Details/{ev.Id}", $"/Shifts/Delete/{shift.Id}");
        Assert.Null(await _factory.WithServiceAsync<web.Repositories.Shifts.Interfaces.IShiftRepository, web.Repositories.Shifts.Dtos.ShiftDto?>(r => r.GetByIdAsync(shift.Id)));
    }

    [Fact]
    public async Task BrugerMedVagter_KanIkkeSlettes()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        var (_, user) = await ParentAsync("Anna");
        using var admin = await AdminAsync();
        await AssignAsync(admin, shift.Id, user.Id);

        await admin.PostFormAsync("/Users", $"/Users/Delete/{user.Id}");
        Assert.Contains("har vagter og kan ikke slettes", await admin.GetHtmlAsync("/Users"));
        Assert.NotNull(await _factory.WithServiceAsync<IUserRepository, UserDto?>(r => r.GetByIdAsync(user.Id)));
    }

    [Fact]
    public async Task Bruger_UdenAdmin_KanIkkeTildele()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        var (client, user) = await ParentAsync("Anna");

        Assert.StartsWith("/Account/AccessDenied", (await client.GetAsync($"/Shifts/Assign/{shift.Id}")).Headers.Location?.PathAndQuery);
        var token = await client.GetAntiforgeryTokenAsync("/Profile");
        var post = await client.PostAsync($"/Shifts/Assign/{shift.Id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["userId"] = user.Id.ToString(), ["__RequestVerificationToken"] = token
        }));
        Assert.StartsWith("/Account/AccessDenied", post.Headers.Location?.PathAndQuery);
        Assert.DoesNotContain(shift.Title, await client.GetHtmlAsync("/MyShifts"));
    }

    [Theory]
    [InlineData("/MyShifts/Cancel")]
    [InlineData("/MyShifts/Delete")]
    [InlineData("/MyShifts/Swap")]
    public async Task Bruger_HarIngenAfmeldEllerByt(string path)
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        var (client, user) = await ParentAsync("Anna");
        using var admin = await AdminAsync();
        await AssignAsync(admin, shift.Id, user.Id);

        var token = await client.GetAntiforgeryTokenAsync("/Profile");
        var response = await client.PostAsync($"{path}/{shift.Id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));

        // Standard-routen svarer 404 eller 405 (ASP.NET Core finder GET-endpoints til stien). Begge er en afvisning.
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed });
        Assert.Contains(shift.Title, await client.GetHtmlAsync("/MyShifts"));
    }
}
