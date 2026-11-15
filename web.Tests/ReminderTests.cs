using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using web.Data;
using web.Data.Models;
using web.Infrastructure.Notifications;
using web.Managers.Notifications;
using web.Managers.Reminders;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.PushSubscriptions.Dtos;
using web.Repositories.PushSubscriptions.Interfaces;
using web.Repositories.Users.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>
/// Påmindelser 48 og 24 timer før vagtstart (krav K19, K20). Uret styres med FakeTimeProvider, som gives direkte til
/// ReminderManager. Hver test bruger sin egen dato, så scanningerne ikke blander testene sammen.
/// </summary>
public class ReminderTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public ReminderTests(WebAppFactory factory) => _factory = factory;

    private Task<T> With<T>(Func<IServiceProvider, Task<T>> action) => _factory.WithServiceAsync<IServiceProvider, T>(action);

    private Task<int> ScanAsync(DateTime nowUtc) =>
        With(async sp =>
        {
            var manager = ActivatorUtilities.CreateInstance<ReminderManager>(sp, new FakeTimeProvider(new DateTimeOffset(nowUtc, TimeSpan.Zero)));
            return (await manager.QueueDueAsync()).QueuedCount;
        });

    /// <summary>Sender køen med samme (styrede) ur som scanningen.</summary>
    private Task<int> SendDueAsync(DateTime nowUtc) =>
        With(sp => ActivatorUtilities.CreateInstance<NotificationManager>(sp, new FakeTimeProvider(new DateTimeOffset(nowUtc, TimeSpan.Zero))).SendDueAsync());

    private sealed record Setup(Guid UserId, Guid ShiftId, Guid AssignmentId, string Endpoint, string Title, string Email);

    /// <summary>En bruger (med push, hvis ønsket) tildelt en kladdevagt, der starter på startUtc.</summary>
    private async Task<Setup> AssignedAsync(DateTime startUtc, bool push = true, bool email = true)
    {
        var user = await _factory.CreateUserAsync($"reminder-{Guid.NewGuid():N}@test.local", displayName: "Rikke Påmindelse");
        var endpoint = FakePushService.NewEndpoint();
        if (push)
        {
            await With(sp => sp.GetRequiredService<IPushSubscriptionRepository>().SaveAsync(new SavePushSubscriptionDto
            {
                UserId = user.Id, Endpoint = endpoint, P256dh = "p256dh-test", Auth = "auth-test"
            }));
        }

        if (!email)
        {
            await With(async sp =>
            {
                await sp.GetRequiredService<IProfileRepository>().SetEmailNotificationsAsync(user.Id, false);
                return true;
            });
        }

        var ev = await _factory.CreateEventAsync(DateOnly.FromDateTime(startUtc));
        var title = $"Kagebod {Guid.NewGuid():N}"[..14];
        var shift = await _factory.CreateShiftAsync(ev.Id, 3, startUtc, title);
        var result = await With(sp => sp.GetRequiredService<IAssignmentRepository>().CreateManyAsync(new CreateAssignmentsDto
        {
            UserId = user.Id,
            ShiftIds = [shift.Id],
            Source = AssignmentSource.Admin,
            OnlyOpen = false
        }));
        Assert.True(result.Succeeded);
        var assignmentId = await With(sp => sp.GetRequiredService<WebDbContext>().Assignments
            .Where(x => x.UserId == user.Id && x.ShiftId == shift.Id).Select(x => x.Id).SingleAsync());
        return new Setup(user.Id, shift.Id, assignmentId, endpoint, title, user.UserName);
    }

    private Task<List<Notification>> RemindersForAsync(Guid userId) =>
        With(sp => sp.GetRequiredService<WebDbContext>().Notifications.AsNoTracking()
            .Where(x => x.UserId == userId && (x.Kind == NotificationKinds.Reminder48 || x.Kind == NotificationKinds.Reminder24))
            .OrderBy(x => x.CreatedAt).ToListAsync());

    [Fact]
    public async Task To_Paamindelser_48og24Timer_PaaPushOgEmail()
    {
        var start = new DateTime(2027, 1, 20, 15, 0, 0, DateTimeKind.Utc); // ons. kl. 16:00 dansk tid
        var setup = await AssignedAsync(start);

        await ScanAsync(start.AddHours(-47));
        var first = await RemindersForAsync(setup.UserId);
        Assert.Equal(2, first.Count);
        Assert.All(first, x => Assert.Equal(NotificationKinds.Reminder48, x.Kind));
        Assert.Contains(first, x => x.Channel == NotificationChannel.Push);
        Assert.Contains(first, x => x.Channel == NotificationChannel.Email);
        Assert.All(first, x => Assert.Equal(setup.AssignmentId, x.AssignmentId));

        await ScanAsync(start.AddHours(-23));
        var all = await RemindersForAsync(setup.UserId);
        Assert.Equal(4, all.Count);
        Assert.Equal(2, all.Count(x => x.Kind == NotificationKinds.Reminder24));

        await SendDueAsync(start.AddHours(-23));
        var pushes = _factory.Push.DeliveredTo(setup.Endpoint);
        Assert.Equal(2, pushes.Count);
        Assert.Equal($"Påmindelse: {setup.Title}", pushes[0].Title);
        Assert.StartsWith("Du har en vagt om 2 dage: ons. 20. jan. 2027 kl. 16:00.", pushes[0].Body);
        Assert.StartsWith("Husk din vagt i morgen kl. 16:00.", pushes[1].Body);
        Assert.Equal($"/MyShifts/Details/{setup.ShiftId}", pushes[0].Url);

        var mails = _factory.Mail.SentTo(setup.Email);
        Assert.Equal(2, mails.Count);
        Assert.Equal("Påmindelse: du har en vagt om 2 dage", mails[0].Subject);
        Assert.Equal("Påmindelse: husk din vagt i morgen", mails[1].Subject);
        Assert.Contains(setup.Title, mails[1].TextBody);
        Assert.Contains($"{WebAppFactory.BaseUrl}/MyShifts/Details/{setup.ShiftId}", mails[1].TextBody);
        Assert.Contains("Se vagten", mails[1].HtmlBody);
    }

    [Fact]
    public async Task GentagneOgParalleleScanninger_GiverIngenDubletter()
    {
        var start = new DateTime(2027, 1, 25, 10, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start);
        var now = start.AddHours(-40);

        await ScanAsync(now);
        await ScanAsync(now);
        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => ScanAsync(now)));

        var reminders = await RemindersForAsync(setup.UserId);
        Assert.Equal(2, reminders.Count);
        Assert.Equal(2, reminders.Select(x => x.DedupKey).Distinct().Count());
    }

    [Fact]
    public async Task TildelingUnder48TimerFoer_FaarKun24TimersPaamindelsen()
    {
        var start = new DateTime(2027, 1, 30, 10, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start);
        await With(sp => sp.GetRequiredService<WebDbContext>().Assignments.Where(x => x.Id == setup.AssignmentId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.CreatedAt, start.AddHours(-30))));

        await ScanAsync(start.AddHours(-29));
        Assert.Empty(await RemindersForAsync(setup.UserId));

        await ScanAsync(start.AddHours(-23));
        var reminders = await RemindersForAsync(setup.UserId);
        Assert.Equal(2, reminders.Count);
        Assert.All(reminders, x => Assert.Equal(NotificationKinds.Reminder24, x.Kind));
    }

    [Fact]
    public async Task AfmeldtTildeling_GiverIngenPaamindelse_OgKoeedeSpringesOver()
    {
        var start = new DateTime(2027, 2, 4, 10, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start);
        await ScanAsync(start.AddHours(-23));
        Assert.Equal(2, (await RemindersForAsync(setup.UserId)).Count);

        var admin = await With(async sp => (await sp.GetRequiredService<IUserRepository>().GetAllAsync()).Single(x => x.UserName == WebAppFactory.AdminUserName));
        await With(sp => sp.GetRequiredService<IAssignmentRepository>().CancelAsync(setup.AssignmentId, admin.Id));
        await SendDueAsync(start.AddHours(-23));
        await ScanAsync(start.AddHours(-22));

        var reminders = await RemindersForAsync(setup.UserId);
        Assert.Equal(2, reminders.Count);
        Assert.All(reminders, x => Assert.Equal(NotificationStatus.Skipped, x.Status));
        Assert.Empty(_factory.Push.DeliveredTo(setup.Endpoint));
        Assert.Empty(_factory.Mail.SentTo(setup.Email));
    }

    [Fact]
    public async Task FlyttetVagt_GiverNyePaamindelser()
    {
        var start = new DateTime(2027, 2, 9, 10, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start);
        await ScanAsync(start.AddHours(-23));

        var moved = start.AddHours(3);
        await With(sp => sp.GetRequiredService<WebDbContext>().Shifts.Where(x => x.Id == setup.ShiftId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.StartUtc, moved).SetProperty(x => x.EndUtc, moved.AddHours(2))));
        await ScanAsync(moved.AddHours(-23));

        var reminders = await RemindersForAsync(setup.UserId);
        Assert.Equal(4, reminders.Count);
        Assert.Equal(4, reminders.Select(x => x.DedupKey).Distinct().Count());
        Assert.Contains(reminders, x => x.DedupKey.Contains(moved.Ticks.ToString()));
    }

    [Fact]
    public async Task StartetVagt_GiverIngenPaamindelse()
    {
        var start = new DateTime(2027, 2, 14, 10, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start);

        await ScanAsync(start.AddMinutes(1));
        await ScanAsync(start.AddHours(-49));

        Assert.Empty(await RemindersForAsync(setup.UserId));
    }

    [Fact]
    public async Task Sommertid_Marts_ViserDanskTid()
    {
        // Sommertid starter søn. 28. marts 2027 kl. 02:00. Vagten er man. 29. marts kl. 10:00 dansk tid (08:00 UTC).
        var start = new DateTime(2027, 3, 29, 8, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start, email: false);

        await ScanAsync(start.AddHours(-47));
        await ScanAsync(start.AddHours(-23));
        await SendDueAsync(start.AddHours(-23));

        var pushes = _factory.Push.DeliveredTo(setup.Endpoint);
        Assert.Equal(2, pushes.Count);
        Assert.StartsWith("Du har en vagt om 2 dage: man. 29. mar. 2027 kl. 10:00.", pushes[0].Body);
        Assert.StartsWith("Husk din vagt i morgen kl. 10:00.", pushes[1].Body);
    }

    [Fact]
    public async Task Sommertid_Oktober_ViserDanskTid()
    {
        // Sommertid slutter søn. 31. oktober 2027 kl. 03:00. Vagten er man. 1. november kl. 10:00 dansk tid (09:00 UTC).
        var start = new DateTime(2027, 11, 1, 9, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start, email: false);

        await ScanAsync(start.AddHours(-47));
        await ScanAsync(start.AddHours(-23));
        await SendDueAsync(start.AddHours(-23));

        var pushes = _factory.Push.DeliveredTo(setup.Endpoint);
        Assert.Equal(2, pushes.Count);
        Assert.StartsWith("Du har en vagt om 2 dage: man. 1. nov. 2027 kl. 10:00.", pushes[0].Body);
        Assert.StartsWith("Husk din vagt i morgen kl. 10:00.", pushes[1].Body);
    }

    [Fact]
    public async Task UdenPushOgMedEmailFra_IngenPaamindelse()
    {
        var start = new DateTime(2027, 2, 19, 10, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start, push: false, email: false);

        await ScanAsync(start.AddHours(-23));

        Assert.Empty(await RemindersForAsync(setup.UserId));
    }

    [Fact]
    public async Task UdenPush_KunEmail()
    {
        var start = new DateTime(2027, 2, 24, 10, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start, push: false);

        await ScanAsync(start.AddHours(-23));

        var reminder = Assert.Single(await RemindersForAsync(setup.UserId));
        Assert.Equal(NotificationChannel.Email, reminder.Channel);
    }

    [Fact]
    public async Task DeaktiveretBruger_FaarIngenPaamindelse()
    {
        var start = new DateTime(2027, 3, 5, 10, 0, 0, DateTimeKind.Utc);
        var setup = await AssignedAsync(start);
        await With(sp => sp.GetRequiredService<IUserRepository>().UpdateAsync(setup.UserId,
            new Repositories.Users.Dtos.UpdateUserDto { DisplayName = "Rikke Påmindelse", IsActive = false }));

        await ScanAsync(start.AddHours(-23));

        Assert.Empty(await RemindersForAsync(setup.UserId));
    }

    [Theory]
    [InlineData(-49, -100, null)]
    [InlineData(-48, -100, "Reminder48")]
    [InlineData(-25, -100, "Reminder48")]
    [InlineData(-24, -100, "Reminder24")]
    [InlineData(-1, -100, "Reminder24")]
    [InlineData(0, -100, null)]
    [InlineData(-30, -47, null)]
    [InlineData(-23, -2, "Reminder24")]
    public void DueKind_Regler(int nowHoursFromStart, int assignedHoursFromStart, string? expected)
    {
        var start = new DateTime(2027, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var assignment = new UpcomingAssignmentDto { StartUtc = start, AssignedAt = start.AddHours(assignedHoursFromStart) };

        Assert.Equal(expected, ReminderManager.DueKind(assignment, start.AddHours(nowHoursFromStart)));
    }
}
