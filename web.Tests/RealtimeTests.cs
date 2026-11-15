using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.SignalR.Client;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>Realtid via SignalR (krav K10, K12, K17): rigtige hub-forbindelser mod testserveren.</summary>
public class RealtimeTests : IClassFixture<WebAppFactory>
{
    private static readonly TimeSpan Expected = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan NotExpected = TimeSpan.FromSeconds(2);

    private readonly WebAppFactory _factory;

    public RealtimeTests(WebAppFactory factory) => _factory = factory;

    /// <summary>En indlogget browser (til POST'er) og en hub-forbindelse med samme login-cookie.</summary>
    private async Task<(HttpClient Browser, HubConnection Hub)> ConnectAsync(string userName, string password)
    {
        var cookies = new CookieContainer();
        var browser = _factory.CreateDefaultClient(new CookieContainerHandler(cookies));
        await browser.LoginAsync(userName, password);
        var hub = BuildHub(cookies.GetCookieHeader(browser.BaseAddress!));
        await hub.StartAsync();
        return (browser, hub);
    }

    private HubConnection BuildHub(string? cookieHeader) =>
        new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "/hubs/shifts"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (cookieHeader is not null)
                {
                    options.Headers["Cookie"] = cookieHeader;
                }
            })
            .Build();

    private async Task<(HttpClient Browser, HubConnection Hub, Guid UserId)> ParentAsync()
    {
        var user = await _factory.CreateUserAsync($"live-{Guid.NewGuid():N}@test.local");
        await _factory.CompleteProfileAsync(user.Id);
        var (browser, hub) = await ConnectAsync(user.UserName, WebAppFactory.UserPassword);
        return (browser, hub, user.Id);
    }

    private static TaskCompletionSource<JsonElement> Listen(HubConnection hub, string name)
    {
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<JsonElement>(name, payload => tcs.TrySetResult(payload));
        return tcs;
    }

    [Fact]
    public async Task UdenLogin_KanManIkkeForbinde()
    {
        await using var hub = BuildHub(cookieHeader: null);
        await Assert.ThrowsAnyAsync<Exception>(() => hub.StartAsync());
    }

    [Fact]
    public async Task Frigivelse_SendesTilAlleIndloggede()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3);
        var (_, parentHub, _) = await ParentAsync();
        await using var _ = parentHub;
        var opened = Listen(parentHub, "shiftsOpened");
        var (admin, adminHub) = await ConnectAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        await using var __ = adminHub;

        await admin.PostFormAsync($"/Events/Open/{ev.Id}", $"/Events/Open/{ev.Id}", ("ShiftIds", shift.Id.ToString()));

        var payload = await opened.Task.WaitAsync(Expected);
        Assert.Equal(ev.Id.ToString(), payload.GetProperty("eventId").GetString());
    }

    [Fact]
    public async Task TildelingAfKladde_NaarKunModtagerenOgAdmin_IkkeAndre()
    {
        var ev = await _factory.CreateEventAsync();
        var draft = await _factory.CreateShiftAsync(ev.Id, 3);
        var (_, receiverHub, receiverId) = await ParentAsync();
        var (_, otherHub, _) = await ParentAsync();
        await using var r = receiverHub;
        await using var o = otherHub;
        var mine = Listen(receiverHub, "myShiftsChanged");
        var otherChanged = Listen(otherHub, "shiftsChanged");
        var otherOpened = Listen(otherHub, "shiftsOpened");
        var otherMine = Listen(otherHub, "myShiftsChanged");
        var (admin, adminHub) = await ConnectAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        await using var a = adminHub;
        var adminEvent = Listen(adminHub, "eventChanged");

        await admin.PostFormAsync($"/Shifts/Assign/{draft.Id}", $"/Shifts/Assign/{draft.Id}", ("userId", receiverId.ToString()));

        await mine.Task.WaitAsync(Expected);
        Assert.Equal(ev.Id.ToString(), (await adminEvent.Task.WaitAsync(Expected)).GetProperty("eventId").GetString());

        // Ingen andre får noget at vide om kladden.
        var leak = await Task.WhenAny(otherChanged.Task, otherOpened.Task, otherMine.Task, Task.Delay(NotExpected));
        Assert.False(leak is Task<JsonElement>, "En anden bruger fik et signal om en kladde.");
    }

    [Fact]
    public async Task Tilmelding_GiverAndreSignalOmAabenVagt()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3);
        await _factory.OpenShiftsAsync(shift.Id);
        var (enroller, enrollerHub, _) = await ParentAsync();
        var (_, watcherHub, _) = await ParentAsync();
        await using var e = enrollerHub;
        await using var w = watcherHub;
        var changed = Listen(watcherHub, "shiftsChanged");
        var mine = Listen(enrollerHub, "myShiftsChanged");

        await enroller.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", shift.Id.ToString()));

        var payload = await changed.Task.WaitAsync(Expected);
        Assert.Contains(shift.Id.ToString(), payload.GetProperty("shiftIds").EnumerateArray().Select(x => x.GetString()));
        await mine.Task.WaitAsync(Expected);
    }

    [Fact]
    public async Task AdminFjernerPerson_PersonenOgAndreFaarBesked()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3);
        await _factory.OpenShiftsAsync(shift.Id);
        var (enroller, enrollerHub, enrollerId) = await ParentAsync();
        var (_, watcherHub, _) = await ParentAsync();
        await using var e = enrollerHub;
        await using var w = watcherHub;
        await enroller.PostFormAsync("/Signup", "/Signup/Enroll", ("ShiftIds", shift.Id.ToString()));

        var mine = Listen(enrollerHub, "myShiftsChanged");
        var changed = Listen(watcherHub, "shiftsChanged");
        var (admin, adminHub) = await ConnectAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);
        await using var a = adminHub;
        var html = await admin.GetHtmlAsync($"/Shifts/Assign/{shift.Id}");
        var assignmentId = System.Text.RegularExpressions.Regex.Match(html, "name=\"assignmentId\" value=\"([0-9a-f-]+)\"").Groups[1].Value;

        await admin.PostFormAsync($"/Shifts/Assign/{shift.Id}", $"/Shifts/Unassign/{shift.Id}", ("assignmentId", assignmentId));

        await mine.Task.WaitAsync(Expected);
        await changed.Task.WaitAsync(Expected);
    }
}
