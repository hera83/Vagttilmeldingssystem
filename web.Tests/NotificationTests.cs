using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using web.Data;
using web.Data.Models;
using web.Managers.Notifications.Dtos;
using web.Managers.Notifications.Interfaces;
using web.Repositories.PushSubscriptions.Dtos;
using web.Repositories.PushSubscriptions.Interfaces;
using web.Repositories.Shifts.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Services.Push;
using web.Services.Push.Dtos.Messages;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>Push ved frigivelse og køen af beskeder (krav K18, K10). NotificationWorker er slået fra, så testene styrer køen selv.</summary>
public class NotificationTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public NotificationTests(WebAppFactory factory) => _factory = factory;

    private Task<T> With<T>(Func<IServiceProvider, Task<T>> action) =>
        _factory.WithServiceAsync<IServiceProvider, T>(action);

    private async Task<(Guid UserId, string UserName)> UserAsync(string? displayName = null)
    {
        var user = await _factory.CreateUserAsync($"push-{Guid.NewGuid():N}@test.local", displayName: displayName);
        return (user.Id, user.UserName);
    }

    private Task<string> SubscribeAsync(Guid userId) =>
        With(async sp =>
        {
            var endpoint = FakePushService.NewEndpoint();
            await sp.GetRequiredService<IPushSubscriptionRepository>().SaveAsync(new SavePushSubscriptionDto
            {
                UserId = userId,
                Endpoint = endpoint,
                P256dh = "p256dh-test",
                Auth = "auth-test"
            });
            return endpoint;
        });

    /// <summary>Den rigtige frigivelse (samme OpenedAt for alle vagterne), som admin laver på siden Åbn for tilmelding.</summary>
    private Task<List<Guid>> OpenForRealAsync(Guid eventId, params Guid[] shiftIds) =>
        With(async sp =>
        {
            var admin = (await sp.GetRequiredService<IUserRepository>().GetAllAsync()).Single(x => x.UserName == WebAppFactory.AdminUserName);
            return await sp.GetRequiredService<IShiftRepository>().OpenAsync(eventId, shiftIds, admin.Id);
        });

    private Task<int> QueueAndSendAsync() =>
        With(async sp =>
        {
            var manager = sp.GetRequiredService<INotificationManager>();
            var queued = await manager.QueueOpenedShiftsAsync();
            await manager.SendDueAsync();
            return queued;
        });

    private Task<List<Notification>> NotificationsForAsync(Guid userId) =>
        With(sp => sp.GetRequiredService<WebDbContext>().Notifications.AsNoTracking().Where(x => x.UserId == userId).ToListAsync());

    [Fact]
    public async Task Frigivelse_GiverEnBeskedPrBrugerMedAbonnement_UdenPersondata()
    {
        var (aId, aName) = await UserAsync("Anne Andersen");
        var aPhone = await SubscribeAsync(aId);
        var aLaptop = await SubscribeAsync(aId);
        var (bId, _) = await UserAsync();
        var bPhone = await SubscribeAsync(bId);
        var (cId, _) = await UserAsync();

        var ev = await _factory.CreateEventAsync();
        var first = await _factory.CreateShiftAsync(ev.Id, 2, title: "Hemmelig titel 1");
        var second = await _factory.CreateShiftAsync(ev.Id, 2, title: "Hemmelig titel 2");
        await OpenForRealAsync(ev.Id, first.Id, second.Id);

        await QueueAndSendAsync();

        Assert.Single(_factory.Push.DeliveredTo(aPhone));
        Assert.Single(_factory.Push.DeliveredTo(aLaptop));
        var message = Assert.Single(_factory.Push.DeliveredTo(bPhone));
        Assert.Equal("Nye vagter er åbne", message.Title);
        Assert.Contains(ev.Name, message.Body);
        Assert.Contains("2 vagter er åbne for", message.Body);
        Assert.Equal($"/Signup?eventId={ev.Id}", message.Url);
        Assert.Equal(PushUrgency.High, message.Urgency);

        // Ingen persondata og ingen vagttitler i beskeden.
        Assert.DoesNotContain("Anne", message.Title + message.Body);
        Assert.DoesNotContain(aName, message.Title + message.Body);
        Assert.DoesNotContain("Hemmelig", message.Title + message.Body);

        // Én besked pr. bruger (ikke pr. enhed), og ingen til brugeren uden abonnement.
        Assert.Single(await NotificationsForAsync(aId), x => x.Kind == "ShiftsOpened" && x.Status == NotificationStatus.Sent);
        Assert.Empty(await NotificationsForAsync(cId));
    }

    [Fact]
    public async Task NytGennemloeb_SenderIkkeIgen()
    {
        var (userId, _) = await UserAsync();
        var endpoint = await SubscribeAsync(userId);
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        await OpenForRealAsync(ev.Id, shift.Id);

        await QueueAndSendAsync();
        await QueueAndSendAsync();
        await QueueAndSendAsync();

        Assert.Single(_factory.Push.DeliveredTo(endpoint));
        Assert.Single(await NotificationsForAsync(userId));
    }

    [Fact]
    public async Task NyFrigivelseIsammeBegivenhed_GiverNyBesked()
    {
        var (userId, _) = await UserAsync();
        var endpoint = await SubscribeAsync(userId);
        var ev = await _factory.CreateEventAsync();
        var first = await _factory.CreateShiftAsync(ev.Id, 2);
        var second = await _factory.CreateShiftAsync(ev.Id, 2);

        await OpenForRealAsync(ev.Id, first.Id);
        await QueueAndSendAsync();
        await Task.Delay(20);
        await OpenForRealAsync(ev.Id, second.Id);
        await QueueAndSendAsync();

        var messages = _factory.Push.DeliveredTo(endpoint);
        Assert.Equal(2, messages.Count);
        Assert.All(messages, x => Assert.Contains("1 vagt er åben for", x.Body));
    }

    [Fact]
    public async Task Kladder_GiverAldrigBeskeder()
    {
        var (userId, _) = await UserAsync();
        var endpoint = await SubscribeAsync(userId);
        var ev = await _factory.CreateEventAsync();
        await _factory.CreateShiftAsync(ev.Id, 2, title: "Kladde der ikke må ses");

        await QueueAndSendAsync();

        Assert.Empty(_factory.Push.DeliveredTo(endpoint));
        Assert.Empty(await NotificationsForAsync(userId));
    }

    [Fact]
    public async Task GammelFrigivelse_MarkeresUdenBesked()
    {
        var (userId, _) = await UserAsync();
        var endpoint = await SubscribeAsync(userId);
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        await OpenForRealAsync(ev.Id, shift.Id);
        await With(sp => sp.GetRequiredService<WebDbContext>().Shifts.Where(x => x.Id == shift.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.OpenedAt, DateTime.UtcNow.AddDays(-2))));

        await QueueAndSendAsync();

        Assert.Empty(_factory.Push.DeliveredTo(endpoint));
        var notifiedAt = await With(sp => sp.GetRequiredService<WebDbContext>().Shifts.Where(x => x.Id == shift.Id).Select(x => x.OpenNotifiedAt).SingleAsync());
        Assert.NotNull(notifiedAt);
    }

    [Fact]
    public async Task UdloebetAbonnement_SlettesOgBeskedenSpringesOver()
    {
        var (userId, _) = await UserAsync();
        var endpoint = await SubscribeAsync(userId);
        _factory.Push.Behaviors[endpoint] = _ => new SendPushResponseDto { Status = PushDeliveryStatus.SubscriptionGone };
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        await OpenForRealAsync(ev.Id, shift.Id);

        await QueueAndSendAsync();

        Assert.Empty(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(userId)));
        Assert.Equal(NotificationStatus.Skipped, Assert.Single(await NotificationsForAsync(userId)).Status);
    }

    [Fact]
    public async Task Fejl_ProeverIgenOgOpgiverEfterTreForsoeg()
    {
        var (userId, _) = await UserAsync();
        var endpoint = await SubscribeAsync(userId);
        _factory.Push.Behaviors[endpoint] = _ => throw new PushException(HttpStatusCode.BadGateway, "Kunne ikke forbinde til push-tjenesten fcm.googleapis.com.");
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        await OpenForRealAsync(ev.Id, shift.Id);

        await QueueAndSendAsync();
        var afterFirst = Assert.Single(await NotificationsForAsync(userId));
        Assert.Equal(NotificationStatus.Pending, afterFirst.Status);
        Assert.Equal(1, afterFirst.Attempts);
        Assert.True(afterFirst.NextAttemptAt > DateTime.UtcNow, "Næste forsøg skal ligge ude i fremtiden.");
        Assert.Contains("fcm.googleapis.com", afterFirst.LastError);
        Assert.DoesNotContain(endpoint, afterFirst.LastError);

        for (var attempt = 2; attempt <= 3; attempt++)
        {
            await With(sp => sp.GetRequiredService<WebDbContext>().Notifications.Where(x => x.Id == afterFirst.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1))));
            await QueueAndSendAsync();
        }

        var final = Assert.Single(await NotificationsForAsync(userId));
        Assert.Equal(NotificationStatus.Failed, final.Status);
        Assert.Equal(3, final.Attempts);
        Assert.Empty(_factory.Push.DeliveredTo(endpoint));
    }

    [Fact]
    public async Task UdenPushNoegler_LaegesIngenBeskederIKoen()
    {
        var (userId, _) = await UserAsync();
        var endpoint = await SubscribeAsync(userId);
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 2);
        await OpenForRealAsync(ev.Id, shift.Id);

        _factory.Push.Enabled = false;
        try
        {
            await QueueAndSendAsync();
        }
        finally
        {
            _factory.Push.Enabled = true;
        }

        await QueueAndSendAsync();
        Assert.Empty(_factory.Push.DeliveredTo(endpoint));
        Assert.Empty(await NotificationsForAsync(userId));
    }

    [Fact]
    public async Task Testbesked_SendesTilBrugerensEnheder()
    {
        var (userId, userName) = await UserAsync();
        var endpoint = await SubscribeAsync(userId);
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(userName, WebAppFactory.UserPassword);

        var response = await PostJsonAsync(client, "/Push/Test", new { });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await With(sp => sp.GetRequiredService<INotificationManager>().SendDueAsync());

        var message = Assert.Single(_factory.Push.DeliveredTo(endpoint));
        Assert.Equal("Testbesked fra Vagttilmelding", message.Title);
    }

    [Fact]
    public async Task Testbesked_UdenEnheder_Giver404()
    {
        var (_, userName) = await UserAsync();
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(userName, WebAppFactory.UserPassword);

        var response = await PostJsonAsync(client, "/Push/Test", new { });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Config_ViserOffentligNoegle()
    {
        var (_, userName) = await UserAsync();
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(userName, WebAppFactory.UserPassword);

        var config = await client.GetFromJsonAsync<Dictionary<string, object>>("/Push/Config");

        Assert.Equal("True", config!["enabled"].ToString(), ignoreCase: true);
        Assert.Equal(FakePushService.PublicKey, config["publicKey"].ToString());
    }

    [Fact]
    public async Task Subscribe_UdenLogin_SenderTilLogin()
    {
        using var client = _factory.CreateBrowser();
        var response = await client.PostAsJsonAsync("/Push/Subscribe", new { endpoint = FakePushService.NewEndpoint(), keys = new { p256dh = "x", auth = "y" } });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Account/Login", response.Headers.Location?.PathAndQuery);
    }

    [Fact]
    public async Task Subscribe_UdenAntiforgeryToken_Giver400()
    {
        var (userId, userName) = await UserAsync();
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(userName, WebAppFactory.UserPassword);

        var response = await client.PostAsJsonAsync("/Push/Subscribe", new { endpoint = FakePushService.NewEndpoint(), keys = new { p256dh = "x", auth = "y" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(userId)));
    }

    [Theory]
    [InlineData("https://evil.example.com/push")]
    [InlineData("http://fcm.googleapis.com/fcm/send/abc")]
    [InlineData("https://fcm.googleapis.com.evil.example.com/abc")]
    [InlineData("https://localhost/abc")]
    public async Task Subscribe_UkendtPushTjeneste_Giver400(string endpoint)
    {
        var (userId, userName) = await UserAsync();
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(userName, WebAppFactory.UserPassword);

        var response = await PostJsonAsync(client, "/Push/Subscribe", new { endpoint, keys = new { p256dh = "x", auth = "y" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(userId)));
    }

    [Fact]
    public async Task Subscribe_GemmerAbonnement_OgUnsubscribeSletterKunEgne()
    {
        var (userId, userName) = await UserAsync();
        var (otherId, _) = await UserAsync();
        var othersEndpoint = await SubscribeAsync(otherId);
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(userName, WebAppFactory.UserPassword);
        var endpoint = FakePushService.NewEndpoint();

        var subscribe = await PostJsonAsync(client, "/Push/Subscribe", new { endpoint, expirationTime = (object?)null, keys = new { p256dh = "p256dh-key", auth = "auth-key" } });
        Assert.Equal(HttpStatusCode.NoContent, subscribe.StatusCode);
        var saved = Assert.Single(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(userId)));
        Assert.Equal(endpoint, saved.Endpoint);

        // En andens abonnement kan ikke slettes.
        var foreign = await PostJsonAsync(client, "/Push/Unsubscribe", new { endpoint = othersEndpoint });
        Assert.Equal(HttpStatusCode.NoContent, foreign.StatusCode);
        Assert.Single(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(otherId)));

        var own = await PostJsonAsync(client, "/Push/Unsubscribe", new { endpoint });
        Assert.Equal(HttpStatusCode.NoContent, own.StatusCode);
        Assert.Empty(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(userId)));
    }

    [Fact]
    public async Task Subscribe_SammeEnhedNyBruger_FlytterAbonnementet()
    {
        var (firstId, _) = await UserAsync();
        var (secondId, _) = await UserAsync();
        var endpoint = await SubscribeAsync(firstId);

        var result = await With(sp => sp.GetRequiredService<INotificationManager>()
            .SubscribeAsync(secondId, new SubscribePushDto { Endpoint = endpoint, P256dh = "a", Auth = "b" }));

        Assert.Equal(SubscribePushResult.Success, result);
        Assert.Empty(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(firstId)));
        Assert.Single(await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().GetByUserIdAsync(secondId)));
    }

    [Fact]
    public async Task MinProfil_HarPushPanel()
    {
        var (_, userName) = await UserAsync();
        using var client = _factory.CreateBrowser();
        await client.LoginAsync(userName, WebAppFactory.UserPassword);

        var html = await client.GetHtmlAsync("/Profile");

        Assert.Contains("data-push-panel", html);
        Assert.Contains("Push-beskeder på denne enhed", html);
        Assert.Contains("/js/push.js", html);
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object body)
    {
        var token = await client.GetAntiforgeryTokenAsync("/Profile");
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("RequestVerificationToken", token);
        return await client.SendAsync(request);
    }
}
