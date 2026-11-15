using Microsoft.Extensions.DependencyInjection;
using web.Data;
using web.Data.Models;
using web.Repositories.Events.Dtos;
using web.Repositories.Events.Interfaces;
using web.Repositories.Shifts.Dtos;
using web.Repositories.Shifts.Interfaces;

namespace web.Tests.Infrastructure;

/// <summary>Opretter testdata direkte via repositories/DbContext (hurtigere end via siderne).</summary>
public static class TestData
{
    public static Task<EventDto> CreateEventAsync(this WebAppFactory factory, DateOnly? date = null, bool archived = false) =>
        factory.WithServiceAsync<IEventRepository, EventDto>(async r =>
        {
            var day = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
            var created = await r.CreateAsync(new CreateEventDto { Name = $"Fest {Guid.NewGuid():N}", Location = "Skolen", StartDate = day, EndDate = day });
            if (archived)
            {
                await r.SetArchivedAsync(created.Id, true);
            }

            return created;
        });

    public static Task<ShiftDto> CreateShiftAsync(this WebAppFactory factory, Guid eventId, int capacity, DateTime? startUtc = null, string? title = null) =>
        factory.WithServiceAsync<IShiftRepository, ShiftDto>(r =>
        {
            var start = startUtc ?? DateTime.UtcNow.Date.AddDays(30).AddHours(15);
            return r.CreateAsync(eventId, new CreateShiftDto
            {
                Title = title ?? $"Vagt {Guid.NewGuid():N}"[..12],
                StartUtc = start,
                EndUtc = start.AddHours(2),
                Capacity = capacity
            });
        });

    /// <summary>Sætter vagter til Open direkte i databasen (frigivelsen kommer i step 06).</summary>
    public static async Task OpenShiftsAsync(this WebAppFactory factory, params Guid[] shiftIds)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WebDbContext>();
        foreach (var shift in db.Shifts.Where(s => shiftIds.Contains(s.Id)))
        {
            shift.Status = ShiftStatus.Open;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>Giver brugeren en komplet profil (krav for tilmelding).</summary>
    public static Task CompleteProfileAsync(this WebAppFactory factory, Guid userId) =>
        factory.WithServiceAsync<web.Repositories.Profiles.Interfaces.IProfileRepository, web.Repositories.Profiles.Dtos.ProfileDto>(r =>
            r.SaveAsync(userId, new web.Repositories.Profiles.Dtos.SaveProfileDto
            {
                FullName = "Test Forælder", Address = "Skolevej 1", Phone = "12345678", ShirtSize = "M",
                DrinkTickets = 2, TivoliSheets = 1, ChildName = "Test Barn", ChildClass = "1.A"
            }));

    /// <summary>Opretter mange brugere hurtigt (uden at hashe en rigtig adgangskode). De kan ikke logge ind.</summary>
    public static async Task<List<Guid>> CreateBulkUsersAsync(this WebAppFactory factory, int count)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<WebDbContext>();
        var users = Enumerable.Range(0, count)
            .Select(i => new User { UserName = $"bulk-{Guid.NewGuid():N}@test.local", DisplayName = $"Bulk {i}", PasswordHash = "x" })
            .ToList();
        db.Users.AddRange(users);
        await db.SaveChangesAsync();
        return users.Select(u => u.Id).ToList();
    }
}
