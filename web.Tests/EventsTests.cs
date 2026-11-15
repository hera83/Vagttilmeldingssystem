using System.Net;
using web.Repositories.Events.Dtos;
using web.Repositories.Events.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

public class EventsTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public EventsTests(WebAppFactory factory) => _factory = factory;

    private async Task<HttpClient> AdminAsync()
    {
        var client = _factory.CreateBrowser();
        await client.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        return client;
    }

    public static (string, string)[] EventForm(string name, string start = "2026-05-16", string end = "2026-05-16", string location = "Skolens festsal") =>
    [
        ("Name", name), ("Location", location), ("StartDate", start), ("EndDate", end), ("Description", "Beskrivelse")
    ];

    private static Guid IdFromLocation(HttpResponseMessage response) =>
        Guid.Parse(response.Headers.Location!.OriginalString.Split('/').Last());

    [Fact]
    public async Task Admin_OpretterBegivenhed_SomVisesIListenOgDetaljer()
    {
        using var admin = await AdminAsync();
        var name = $"Skolefest {Guid.NewGuid():N}";
        var response = await admin.PostFormAsync("/Events/Create", "/Events/Create", EventForm(name));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var id = IdFromLocation(response);

        Assert.Contains(name, await admin.GetHtmlAsync("/Events"));
        var details = await admin.GetHtmlAsync($"/Events/Details/{id}");
        Assert.Contains(name, details);
        Assert.Contains("lør. 16. maj 2026", details);
    }

    [Fact]
    public async Task Slutdato_FoerStartdato_Afvises()
    {
        using var admin = await AdminAsync();
        var response = await admin.PostFormAsync("/Events/Create", "/Events/Create", EventForm("Forkert periode", start: "2026-05-16", end: "2026-05-15"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Slutdatoen må ikke ligge før startdatoen.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Navn_OgDatoer_ErPaakraevet()
    {
        using var admin = await AdminAsync();
        var response = await admin.PostFormAsync("/Events/Create", "/Events/Create",
            ("Name", ""), ("Location", ""), ("StartDate", ""), ("EndDate", ""));
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Skriv et navn på begivenheden.", html);
        Assert.Contains("Vælg en startdato.", html);
        Assert.Contains("Skriv, hvor begivenheden foregår.", html);
    }

    [Fact]
    public async Task Admin_RetterBegivenhed()
    {
        using var admin = await AdminAsync();
        var created = await admin.PostFormAsync("/Events/Create", "/Events/Create", EventForm($"Før {Guid.NewGuid():N}"));
        var id = IdFromLocation(created);
        var newName = $"Efter {Guid.NewGuid():N}";
        var response = await admin.PostFormAsync($"/Events/Edit/{id}", $"/Events/Edit/{id}", EventForm(newName, start: "2026-06-01", end: "2026-06-02"));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var item = await _factory.WithServiceAsync<IEventRepository, EventDto?>(r => r.GetByIdAsync(id));
        Assert.Equal(newName, item!.Name);
        Assert.Equal(new DateOnly(2026, 6, 2), item.EndDate);
    }

    [Fact]
    public async Task Arkivering_FlytterBegivenhedenMellemFanerne_OgSoegningVirker()
    {
        using var admin = await AdminAsync();
        var token = Guid.NewGuid().ToString("N")[..8];
        var name = $"Arbejdslørdag {token}";
        var id = IdFromLocation(await admin.PostFormAsync("/Events/Create", "/Events/Create", EventForm(name)));

        Assert.Contains(name, await admin.GetHtmlAsync($"/Events?q={token}"));
        Assert.DoesNotContain(name, await admin.GetHtmlAsync("/Events?q=findesikke"));

        var archive = await admin.PostFormAsync($"/Events/Details/{id}", $"/Events/Archive/{id}");
        Assert.Equal(HttpStatusCode.Redirect, archive.StatusCode);
        Assert.DoesNotContain(name, await admin.GetHtmlAsync("/Events"));
        Assert.Contains(name, await admin.GetHtmlAsync("/Events?archived=true"));

        await admin.PostFormAsync($"/Events/Details/{id}", $"/Events/Unarchive/{id}");
        Assert.Contains(name, await admin.GetHtmlAsync("/Events"));
    }

    [Fact]
    public async Task Admin_SletterBegivenhed()
    {
        using var admin = await AdminAsync();
        var id = IdFromLocation(await admin.PostFormAsync("/Events/Create", "/Events/Create", EventForm($"Slet {Guid.NewGuid():N}")));
        var response = await admin.PostFormAsync($"/Events/Details/{id}", $"/Events/Delete/{id}");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/Events/Details/{id}")).StatusCode);
    }

    [Fact]
    public async Task Bruger_UdenAdmin_KanHverkenSeEllerAendreBegivenheder()
    {
        var user = await _factory.CreateUserAsync($"bruger-{Guid.NewGuid():N}@test.local");
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(user.UserName, WebAppFactory.UserPassword);

        var get = await client.GetAsync("/Events");
        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", get.Headers.Location?.PathAndQuery);

        var token = await client.GetAntiforgeryTokenAsync("/Profile");
        var post = await client.PostAsync("/Events/Create", new FormUrlEncodedContent(
            EventForm("Hacket").Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2))
                .Append(new KeyValuePair<string, string>("__RequestVerificationToken", token))));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.StartsWith("/Account/AccessDenied", post.Headers.Location?.PathAndQuery);

        var all = await _factory.WithServiceAsync<IEventRepository, List<EventDto>>(r => r.SearchAsync(new EventSearchDto { Text = "Hacket" }));
        Assert.Empty(all);
    }
}
