using System.Net;
using web.Data.Models;
using web.Repositories.Events.Dtos;
using web.Repositories.Events.Interfaces;
using web.Repositories.Shifts.Dtos;
using web.Repositories.Shifts.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

public class ShiftsTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public ShiftsTests(WebAppFactory factory) => _factory = factory;

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateBrowser();
        await client.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        return client;
    }

    private Task<EventDto> CreateEventAsync(DateOnly start, DateOnly? end = null) =>
        _factory.WithServiceAsync<IEventRepository, EventDto>(r => r.CreateAsync(new CreateEventDto
        {
            Name = $"Fest {Guid.NewGuid():N}", Location = "Skolen", StartDate = start, EndDate = end ?? start
        }));

    private Task<List<ShiftDto>> ShiftsOfAsync(Guid eventId) =>
        _factory.WithServiceAsync<IShiftRepository, List<ShiftDto>>(r => r.GetByEventIdAsync(eventId));

    private static (string, string)[] ShiftForm(string date, string start = "17:00", string end = "19:00", string capacity = "4", string title = "Kiosk") =>
        [("Title", title), ("Date", date), ("StartTime", start), ("EndTime", end), ("Capacity", capacity), ("Description", "Ved hovedindgangen")];

    private static async Task<HttpResponseMessage> PostShiftAsync(HttpClient admin, Guid eventId, params (string, string)[] fields) =>
        await admin.PostFormAsync($"/Shifts/Create?eventId={eventId}", $"/Shifts/Create?eventId={eventId}", fields);

    [Fact]
    public async Task NyVagt_ErAltidKladde_OgsaaNaarFormularenSigerOpen()
    {
        var ev = await CreateEventAsync(new DateOnly(2027, 6, 1));
        using var admin = await AdminAsync();
        var fields = ShiftForm("2027-06-01").Append(("Status", "Open")).ToArray();
        var response = await PostShiftAsync(admin, ev.Id, fields);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var shift = Assert.Single(await ShiftsOfAsync(ev.Id));
        Assert.Equal(ShiftStatus.Draft, shift.Status);
        Assert.Contains("Skjult – kladde", await admin.GetHtmlAsync($"/Events/Details/{ev.Id}"));
    }

    [Fact]
    public async Task DanskTid_GemmesSomUtc_SommerOgVinter()
    {
        var summer = await CreateEventAsync(new DateOnly(2027, 6, 1));
        var winter = await CreateEventAsync(new DateOnly(2027, 1, 15));
        using var admin = await AdminAsync();
        await PostShiftAsync(admin, summer.Id, ShiftForm("2027-06-01", "17:00", "19:00"));
        await PostShiftAsync(admin, winter.Id, ShiftForm("2027-01-15", "17:00", "19:00"));

        var s = Assert.Single(await ShiftsOfAsync(summer.Id));
        Assert.Equal(new DateTime(2027, 6, 1, 15, 0, 0), s.StartUtc);
        var w = Assert.Single(await ShiftsOfAsync(winter.Id));
        Assert.Equal(new DateTime(2027, 1, 15, 16, 0, 0), w.StartUtc);
    }

    [Fact]
    public async Task VagtOverMidnat_SlutterNaesteDag()
    {
        var ev = await CreateEventAsync(new DateOnly(2027, 6, 1));
        using var admin = await AdminAsync();
        var response = await PostShiftAsync(admin, ev.Id, ShiftForm("2027-06-01", "22:00", "01:00", title: "Oprydning"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var shift = Assert.Single(await ShiftsOfAsync(ev.Id));
        Assert.Equal(TimeSpan.FromHours(3), shift.EndUtc - shift.StartUtc);
        Assert.Contains("22:00 – 01:00 (næste dag)", await admin.GetHtmlAsync($"/Events/Details/{ev.Id}"));
    }

    [Theory]
    [InlineData("0", "17:00", "19:00", "Antal pladser skal være mellem 1 og 500.")]
    [InlineData("501", "17:00", "19:00", "Antal pladser skal være mellem 1 og 500.")]
    [InlineData("4", "17:00", "17:00", "Sluttidspunktet skal være et andet end starttidspunktet.")]
    [InlineData("4", "08:00", "21:00", "En vagt kan højst vare 12 timer.")]
    public async Task UgyldigeVaerdier_Afvises(string capacity, string start, string end, string message)
    {
        var ev = await CreateEventAsync(new DateOnly(2027, 6, 1));
        using var admin = await AdminAsync();
        var response = await PostShiftAsync(admin, ev.Id, ShiftForm("2027-06-01", start, end, capacity));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(message, await response.Content.ReadAsStringAsync());
        Assert.Empty(await ShiftsOfAsync(ev.Id));
    }

    [Fact]
    public async Task DatoUdenforBegivenhedensPeriode_Afvises()
    {
        var ev = await CreateEventAsync(new DateOnly(2027, 6, 1), new DateOnly(2027, 6, 2));
        using var admin = await AdminAsync();
        var response = await PostShiftAsync(admin, ev.Id, ShiftForm("2027-06-05"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Datoen skal ligge i begivenhedens periode", await response.Content.ReadAsStringAsync());
        Assert.Empty(await ShiftsOfAsync(ev.Id));
    }

    [Fact]
    public async Task TidspunktDerIkkeFindesVedSkiftTilSommertid_Afvises()
    {
        // Sommertid 2027 starter søndag 28. marts kl. 02:00 → 03:00. Kl. 02:30 findes ikke.
        var ev = await CreateEventAsync(new DateOnly(2027, 3, 28));
        using var admin = await AdminAsync();
        var response = await PostShiftAsync(admin, ev.Id, ShiftForm("2027-03-28", "02:30", "04:00"));
        Assert.Contains("Starttidspunktet findes ikke den dag på grund af skift til sommertid.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_RetterOgSletterVagt_StatusAendresIkke()
    {
        var ev = await CreateEventAsync(new DateOnly(2027, 6, 1));
        using var admin = await AdminAsync();
        await PostShiftAsync(admin, ev.Id, ShiftForm("2027-06-01"));
        var shift = Assert.Single(await ShiftsOfAsync(ev.Id));

        var edit = await admin.PostFormAsync($"/Shifts/Edit/{shift.Id}", $"/Shifts/Edit/{shift.Id}",
            ShiftForm("2027-06-01", "18:00", "20:00", "6", "Bar").Append(("Status", "Open")).ToArray());
        Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
        var updated = Assert.Single(await ShiftsOfAsync(ev.Id));
        Assert.Equal("Bar", updated.Title);
        Assert.Equal(6, updated.Capacity);
        Assert.Equal(ShiftStatus.Draft, updated.Status);

        var delete = await admin.PostFormAsync($"/Events/Details/{ev.Id}", $"/Shifts/Delete/{shift.Id}");
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);
        Assert.Empty(await ShiftsOfAsync(ev.Id));
    }

    [Fact]
    public async Task BegivenhedMedVagter_KanIkkeSlettes()
    {
        var ev = await CreateEventAsync(new DateOnly(2027, 6, 1));
        using var admin = await AdminAsync();
        await PostShiftAsync(admin, ev.Id, ShiftForm("2027-06-01"));

        var response = await admin.PostFormAsync($"/Events/Details/{ev.Id}", $"/Events/Delete/{ev.Id}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("kan ikke slettes", await admin.GetHtmlAsync($"/Events/Details/{ev.Id}"));
        Assert.NotNull(await _factory.WithServiceAsync<IEventRepository, EventDto?>(r => r.GetByIdAsync(ev.Id)));
    }

    [Fact]
    public async Task Statusfilter_ViserAntal()
    {
        var ev = await CreateEventAsync(new DateOnly(2027, 6, 1));
        using var admin = await AdminAsync();
        await PostShiftAsync(admin, ev.Id, ShiftForm("2027-06-01", title: "Kiosk"));
        await PostShiftAsync(admin, ev.Id, ShiftForm("2027-06-01", "19:00", "21:00", title: "Bar"));

        var html = await admin.GetHtmlAsync($"/Events/Details/{ev.Id}?status=Draft");
        Assert.Contains("Kiosk", html);
        Assert.Contains("Bar", html);
        Assert.DoesNotContain("Kiosk", await admin.GetHtmlAsync($"/Events/Details/{ev.Id}?status=Open"));
    }

    [Fact]
    public async Task Bruger_UdenAdmin_KanIkkeOpretteVagter()
    {
        var ev = await CreateEventAsync(new DateOnly(2027, 6, 1));
        var user = await _factory.CreateUserAsync($"bruger-{Guid.NewGuid():N}@test.local");
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(user.UserName, WebAppFactory.UserPassword);

        Assert.StartsWith("/Account/AccessDenied", (await client.GetAsync($"/Shifts/Create?eventId={ev.Id}")).Headers.Location?.PathAndQuery);
        var token = await client.GetAntiforgeryTokenAsync("/Profile");
        var post = await client.PostAsync($"/Shifts/Create?eventId={ev.Id}", new FormUrlEncodedContent(
            ShiftForm("2027-06-01").Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2))
                .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));
        Assert.StartsWith("/Account/AccessDenied", post.Headers.Location?.PathAndQuery);
        Assert.Empty(await ShiftsOfAsync(ev.Id));
    }
}
