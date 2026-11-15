using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using web.Data.Models;
using web.Extensions;
using web.Managers.Dashboard.Dtos;
using web.Managers.Dashboard.Interfaces;
using web.Repositories.Profiles.Dtos;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Shifts.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>Admin-dashboardet og deltagerlisten (step 13, krav K21). Tallene tjekkes som ændringer, fordi testene deler database.</summary>
public class DashboardTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public DashboardTests(WebAppFactory factory) => _factory = factory;

    private Task<DashboardDto> DashboardAsync() => _factory.WithServiceAsync<IDashboardManager, DashboardDto>(m => m.GetAsync());

    private async Task<HttpClient> AdminAsync()
    {
        var admin = _factory.CreateBrowser();
        await admin.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        return admin;
    }

    private async Task<HttpClient> ParentAsync()
    {
        var user = await _factory.CreateUserAsync($"dash-{Guid.NewGuid():N}@test.local");
        await _factory.CompleteProfileAsync(user.Id);
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(user.UserName, WebAppFactory.UserPassword);
        return browser;
    }

    [Fact]
    public async Task Tal_FoelgerDatabasen_VedTilmeldingOgFjernelse_OgKladderErIkkeAabne()
    {
        var before = await DashboardAsync();
        var ev = await _factory.CreateEventAsync();
        var open = await _factory.CreateShiftAsync(ev.Id, 3);
        await _factory.CreateShiftAsync(ev.Id, 2);
        await _factory.OpenShiftsAsync(open.Id);

        var created = await DashboardAsync();
        Assert.Equal(before.ActiveEventCount + 1, created.ActiveEventCount);
        Assert.Equal(before.Shifts.OpenShiftCount + 1, created.Shifts.OpenShiftCount);
        Assert.Equal(before.Shifts.DraftShiftCount + 1, created.Shifts.DraftShiftCount);
        Assert.Equal(before.Shifts.OpenFreeSeats + 3, created.Shifts.OpenFreeSeats);
        Assert.Equal(before.Shifts.UpcomingCapacity + 5, created.Shifts.UpcomingCapacity);
        var row = Assert.Single(created.UpcomingEvents.Concat(created.EventsWithDrafts).Where(x => x.Event.Id == ev.Id).DistinctBy(x => x.Event.Id));
        Assert.Equal((2, 1, 1, 5, 0), (row.ShiftCount, row.DraftCount, row.OpenCount, row.Capacity, row.Taken));
        Assert.Contains(created.EventsWithDrafts, x => x.Event.Id == ev.Id);

        using var parent = await ParentAsync();
        await parent.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", open.Id.ToString()));
        var enrolled = await DashboardAsync();
        Assert.Equal(created.Shifts.UpcomingTaken + 1, enrolled.Shifts.UpcomingTaken);
        Assert.Equal(created.Shifts.OpenFreeSeats - 1, enrolled.Shifts.OpenFreeSeats);
        Assert.Contains(enrolled.Activity, x => x.Kind == DashboardActivityKind.Enrolled && x.EventId == ev.Id);

        using var admin = await AdminAsync();
        var assignHtml = await admin.GetHtmlAsync($"/Shifts/Assign/{open.Id}");
        var assignmentId = System.Text.RegularExpressions.Regex.Match(assignHtml, "name=\"assignmentId\" value=\"([0-9a-f-]+)\"").Groups[1].Value;
        await admin.PostFormAsync($"/Shifts/Assign/{open.Id}", $"/Shifts/Unassign/{open.Id}", ("assignmentId", assignmentId));
        var removed = await DashboardAsync();
        Assert.Equal(created.Shifts.UpcomingTaken, removed.Shifts.UpcomingTaken);
        Assert.Equal(created.Shifts.OpenFreeSeats, removed.Shifts.OpenFreeSeats);
        Assert.Contains(removed.Activity, x => x.Kind == DashboardActivityKind.Removed && x.EventId == ev.Id);
    }

    [Fact]
    public async Task Frigivelse_VisesISenesteAktivitet()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        await _factory.WithServiceAsync<IServiceProvider, List<Guid>>(async sp =>
        {
            var admin = (await sp.GetRequiredService<IUserRepository>().GetAllAsync()).Single(x => x.UserName == WebAppFactory.AdminUserName);
            return await sp.GetRequiredService<IShiftRepository>().OpenAsync(ev.Id, [shift.Id], admin.Id);
        });

        var dashboard = await DashboardAsync();

        var opened = Assert.Single(dashboard.Activity, x => x.Kind == DashboardActivityKind.Opened && x.EventId == ev.Id);
        Assert.Equal(1, opened.ShiftCount);
        Assert.Equal(WebAppFactory.AdminUserName, opened.ByName);
    }

    [Fact]
    public async Task Side_ViserOverblikForAdmin()
    {
        var ev = await _factory.CreateEventAsync();
        await _factory.CreateShiftAsync(ev.Id, 2);
        using var admin = await AdminAsync();

        var html = await admin.GetHtmlAsync("/Dashboard");

        Assert.Contains("Aktive begivenheder", html);
        Assert.Contains("Kommende begivenheder", html);
        Assert.Contains("Seneste aktivitet", html);
        Assert.Contains("Beskeder og påmindelser", html);
        Assert.Contains("Slået fra (Reminders:Enabled)", html);
        Assert.Contains($"href=\"/Events/Open/{ev.Id}\"", html);
        Assert.Contains("data-live-region=\"dashboard\"", html);
    }

    [Fact]
    public async Task BrugerUdenAdmin_FaarIngenAdgang()
    {
        var ev = await _factory.CreateEventAsync();
        using var parent = await ParentAsync();

        foreach (var url in new[] { "/Dashboard", $"/Events/Participants/{ev.Id}" })
        {
            var response = await parent.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/Account/AccessDenied", response.Headers.Location?.PathAndQuery);
        }

        // Brugerens egen forside er stadig forsiden.
        Assert.Equal(HttpStatusCode.OK, (await parent.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task Deltagerliste_CsvMedProfilerOgEscaping()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 5, title: "Bod; kage \"special\"");
        await _factory.OpenShiftsAsync(shift.Id);
        var user = await _factory.CreateUserAsync($"csv-{Guid.NewGuid():N}@test.local");
        await _factory.WithServiceAsync<IProfileRepository, ProfileDto>(r => r.SaveAsync(user.Id, new SaveProfileDto
        {
            FullName = "Søren Ærø-Åby", Address = "Skolevej 1", Phone = "+45 12 34 56 78", ShirtSize = "XL",
            DrinkTickets = 4, TivoliSheets = 2, ChildName = "=HYPERLINK(\"x\")", ChildClass = "6.A"
        }));
        using var browser = _factory.CreateBrowser();
        await browser.LoginAsync(user.UserName, WebAppFactory.UserPassword);
        await browser.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", shift.Id.ToString()));
        using var admin = await AdminAsync();

        var response = await admin.GetAsync($"/Events/Participants/{ev.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("deltagerliste-fest-", response.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Vagt;Dato;Tid;Navn;Telefon;E-mail;Trøjestørrelse;Væskebilletter;Tivoliark;Barnets navn;Klasse;Tilmeldt", lines[0]);
        var row = Assert.Single(lines[1..]);
        Assert.StartsWith("\"Bod; kage \"\"special\"\"\";", row);
        Assert.Contains(";Søren Ærø-Åby;'+45 12 34 56 78;" + user.UserName + ";XL;4;2;\"'=HYPERLINK(\"\"x\"\")\";6.A;Selv", row);
    }

    [Fact]
    public async Task Deltagerliste_UkendtBegivenhed_Giver404()
    {
        using var admin = await AdminAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/Events/Participants/{Guid.NewGuid()}")).StatusCode);
    }

    [Theory]
    [InlineData("tekst", "tekst")]
    [InlineData("a;b", "\"a;b\"")]
    [InlineData("sagde \"hej\"", "\"sagde \"\"hej\"\"\"")]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("-2", "'-2")]
    [InlineData("linje\nto", "\"linje\nto\"")]
    [InlineData(null, "")]
    public void CsvHelper_Escape(string? value, string expected) => Assert.Equal(expected, CsvHelper.Escape(value));

    [Theory]
    [InlineData("Skolefest 2027", "skolefest-2027")]
    [InlineData("Ærø Ølbod & Åby!", "aeroe-oelbod-aaby")]
    [InlineData("  ", "fil")]
    public void FilnavnSlug(string value, string expected) => Assert.Equal(expected, value.ToFileNameSlug());

    [Fact]
    public void RelativDanskTid()
    {
        var now = new DateTime(2027, 3, 12, 12, 0, 0, DateTimeKind.Utc); // fre. kl. 13:00 dansk tid
        Assert.Equal("I dag kl. 10:24", new DateTime(2027, 3, 12, 9, 24, 0, DateTimeKind.Utc).ToDanishRelativeText(now));
        Assert.Equal("I går kl. 16:47", new DateTime(2027, 3, 11, 15, 47, 0, DateTimeKind.Utc).ToDanishRelativeText(now));
        Assert.Equal("I morgen kl. 08:00", new DateTime(2027, 3, 13, 7, 0, 0, DateTimeKind.Utc).ToDanishRelativeText(now));
        Assert.Equal("ons. 3. mar. kl. 09:00", new DateTime(2027, 3, 3, 8, 0, 0, DateTimeKind.Utc).ToDanishRelativeText(now));
        Assert.Equal("tir. 1. dec. 2026 kl. 09:00", new DateTime(2026, 12, 1, 8, 0, 0, DateTimeKind.Utc).ToDanishRelativeText(now));
    }
}
