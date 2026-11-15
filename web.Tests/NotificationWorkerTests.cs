using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using web.Infrastructure.Notifications;
using web.Repositories.PushSubscriptions.Dtos;
using web.Repositories.PushSubscriptions.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>Hele vejen: admin åbner for tilmelding på siden → NotificationWorker vækkes → push sendes (krav K18).</summary>
public class NotificationWorkerTests : IClassFixture<WorkerWebAppFactory>
{
    private readonly WorkerWebAppFactory _factory;

    public NotificationWorkerTests(WorkerWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Frigivelse_PaaSiden_SenderPushViaWorkeren()
    {
        var user = await _factory.CreateUserAsync($"worker-{Guid.NewGuid():N}@test.local");
        var endpoint = FakePushService.NewEndpoint();
        await _factory.WithServiceAsync<IPushSubscriptionRepository, PushSubscriptionDto>(r => r.SaveAsync(new SavePushSubscriptionDto
        {
            UserId = user.Id,
            Endpoint = endpoint,
            P256dh = "p256dh-test",
            Auth = "auth-test"
        }));
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 3);
        using var admin = _factory.CreateBrowser();
        await admin.LoginAsync(WebAppFactory.AdminUserName, WebAppFactory.AdminPassword);

        await admin.PostFormAsync($"/Events/Open/{ev.Id}", $"/Events/Open/{ev.Id}", ("ShiftIds", shift.Id.ToString()));

        var watch = Stopwatch.StartNew();
        while (_factory.Push.DeliveredTo(endpoint).Count == 0 && watch.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Delay(100);
        }

        var message = Assert.Single(_factory.Push.DeliveredTo(endpoint));
        Assert.Contains(ev.Name, message.Body);
    }

    [Fact]
    public async Task ReminderWorker_ScannerVedOpstart_OgViserStatus()
    {
        _ = _factory.Server; // starter appen og dens workers
        var status = _factory.Services.GetRequiredService<ReminderWorkerStatus>();

        var watch = Stopwatch.StartNew();
        while (status.Current.RunCount == 0 && watch.Elapsed < TimeSpan.FromSeconds(10))
        {
            await Task.Delay(100);
        }

        var current = status.Current;
        Assert.True(current.Enabled);
        Assert.Equal(TimeSpan.FromHours(1), current.Interval);
        Assert.True(current.RunCount >= 1);
        Assert.NotNull(current.LastRunCompletedAt);
        Assert.Null(current.LastError);
        Assert.Equal(current.LastRunStartedAt!.Value.AddHours(1), current.NextRunAt);
    }
}
