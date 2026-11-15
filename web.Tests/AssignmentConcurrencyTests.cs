using Microsoft.Extensions.DependencyInjection;
using web.Data.Models;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Tests.Infrastructure;

namespace web.Tests;

/// <summary>
/// Først-til-mølle under samtidighed (krav K13/K14) mod en rigtig SQLite-fil. Hver opgave har sit eget scope,
/// altså sin egen DbContext og databaseforbindelse, ligesom samtidige requests.
/// </summary>
public class AssignmentConcurrencyTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public AssignmentConcurrencyTests(WebAppFactory factory) => _factory = factory;

    private async Task<AssignResultDto> AssignAsync(Guid userId, AssignmentSource source, bool onlyOpen, params Guid[] shiftIds)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAssignmentRepository>();
        return await repository.CreateManyAsync(new CreateAssignmentsDto
        {
            UserId = userId, ShiftIds = [.. shiftIds], Source = source, OnlyOpen = onlyOpen
        });
    }

    private async Task<int> ActiveCountAsync(Guid shiftId) =>
        (await _factory.WithServiceAsync<IAssignmentRepository, Dictionary<Guid, int>>(r => r.GetActiveCountsAsync([shiftId])))
        .GetValueOrDefault(shiftId);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task FemtiSamtidigeForsoeg_GiverPraeciseLigeSaaMangeSomPladser(int capacity)
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, capacity);
        var users = await _factory.CreateBulkUsersAsync(50);

        var results = await Task.WhenAll(users.Select(u => Task.Run(() => AssignAsync(u, AssignmentSource.Admin, false, shift.Id))));

        Assert.Equal(capacity, results.Count(r => r.Succeeded));
        Assert.All(results.Where(r => !r.Succeeded), r => Assert.Equal(AssignFailureReason.Full, Assert.Single(r.Failures).Reason));
        Assert.Equal(capacity, await ActiveCountAsync(shift.Id));
    }

    [Fact]
    public async Task SammeBrugerTiGangeSamtidig_GiverEnTildeling()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 5);
        var user = (await _factory.CreateBulkUsersAsync(1))[0];

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => AssignAsync(user, AssignmentSource.Admin, false, shift.Id))));

        Assert.Single(results, r => r.Succeeded);
        Assert.Equal(1, await ActiveCountAsync(shift.Id));
    }

    [Fact]
    public async Task FlereVagter_EnErFuld_IngenTildeles()
    {
        var ev = await _factory.CreateEventAsync();
        var a = await _factory.CreateShiftAsync(ev.Id, 2);
        var full = await _factory.CreateShiftAsync(ev.Id, 1);
        var c = await _factory.CreateShiftAsync(ev.Id, 2);
        var users = await _factory.CreateBulkUsersAsync(2);
        Assert.True((await AssignAsync(users[0], AssignmentSource.Admin, false, full.Id)).Succeeded);

        var result = await AssignAsync(users[1], AssignmentSource.Self, false, a.Id, full.Id, c.Id);

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal(full.Id, failure.ShiftId);
        Assert.Equal(AssignFailureReason.Full, failure.Reason);
        Assert.Equal(0, await ActiveCountAsync(a.Id));
        Assert.Equal(0, await ActiveCountAsync(c.Id));
    }

    [Fact]
    public async Task ParalleleMultivalgMedOverlap_OverbookerAldrig()
    {
        var ev = await _factory.CreateEventAsync();
        var shifts = new[]
        {
            await _factory.CreateShiftAsync(ev.Id, 1), await _factory.CreateShiftAsync(ev.Id, 1), await _factory.CreateShiftAsync(ev.Id, 1)
        };
        var users = await _factory.CreateBulkUsersAsync(30);

        // Hver bruger vil have to af de tre vagter, i forskellig rækkefølge.
        var results = await Task.WhenAll(users.Select((u, i) => Task.Run(() =>
            AssignAsync(u, AssignmentSource.Self, false, shifts[i % 3].Id, shifts[(i + 1) % 3].Id))));

        foreach (var shift in shifts)
        {
            Assert.True(await ActiveCountAsync(shift.Id) <= 1, "En vagt med 1 plads må aldrig få mere end 1.");
        }

        // Hver vellykket tilmelding fik begge sine vagter (alt eller intet).
        Assert.All(results.Where(r => r.Succeeded), r => Assert.Equal(2, r.CreatedShiftIds.Count));
        Assert.Equal(30, results.Length);
    }

    [Fact]
    public async Task OnlyOpen_AfviserKladderOgArkiveredeBegivenheder()
    {
        var ev = await _factory.CreateEventAsync();
        var draft = await _factory.CreateShiftAsync(ev.Id, 3);
        var archivedEvent = await _factory.CreateEventAsync(archived: true);
        var archivedShift = await _factory.CreateShiftAsync(archivedEvent.Id, 3);
        await _factory.OpenShiftsAsync(archivedShift.Id);
        var user = (await _factory.CreateBulkUsersAsync(1))[0];

        var result = await AssignAsync(user, AssignmentSource.Self, true, draft.Id, archivedShift.Id);

        Assert.False(result.Succeeded);
        Assert.All(result.Failures, f => Assert.Equal(AssignFailureReason.NotOpen, f.Reason));
        Assert.Equal(2, result.Failures.Count);
    }

    [Fact]
    public async Task AdminTildeling_TaellerMedIKapaciteten()
    {
        var ev = await _factory.CreateEventAsync();
        var shift = await _factory.CreateShiftAsync(ev.Id, 1);
        await _factory.OpenShiftsAsync(shift.Id);
        var users = await _factory.CreateBulkUsersAsync(2);

        Assert.True((await AssignAsync(users[0], AssignmentSource.Admin, false, shift.Id)).Succeeded);
        var self = await AssignAsync(users[1], AssignmentSource.Self, true, shift.Id);

        Assert.Equal(AssignFailureReason.Full, Assert.Single(self.Failures).Reason);
    }
}
