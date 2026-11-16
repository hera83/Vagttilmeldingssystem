using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Models;
using web.Repositories.Assignments.Dtos;
using web.Repositories.Assignments.Interfaces;
using web.Repositories.Assignments.Mappers;

namespace web.Repositories.Assignments;

public class AssignmentRepository(WebDbContext dbContext) : IAssignmentRepository
{
    public async Task<AssignResultDto> CreateManyAsync(CreateAssignmentsDto dto, CancellationToken cancellationToken = default)
    {
        var result = new AssignResultDto();
        var ids = dto.ShiftIds.Distinct().Order().ToList();
        if (ids.Count == 0)
        {
            return result;
        }

        // Microsoft.Data.Sqlite starter transaktionen med BEGIN IMMEDIATE: der er kun én skriver ad gangen,
        // så "tæl aktive pladser" og "indsæt" kan ikke blandes med en anden tilmelding. Andre requests venter
        // (busy timeout) i stedet for at fejle. Ingen lock i C#, så det virker også på tværs af processer.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var shifts = await (
                from s in dbContext.Shifts
                join e in dbContext.Events on s.EventId equals e.Id
                where ids.Contains(s.Id)
                select new { s.Id, s.Capacity, s.Status, s.StartUtc, EventArchived = e.IsArchived })
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var activeCounts = await dbContext.Assignments
            .Where(a => ids.Contains(a.ShiftId) && a.IsActive)
            .GroupBy(a => a.ShiftId)
            .Select(g => new { ShiftId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShiftId, x => x.Count, cancellationToken);

        var alreadyMine = (await dbContext.Assignments
            .Where(a => ids.Contains(a.ShiftId) && a.UserId == dto.UserId && a.IsActive)
            .Select(a => a.ShiftId)
            .ToListAsync(cancellationToken)).ToHashSet();

        foreach (var id in ids)
        {
            var shift = shifts.FirstOrDefault(x => x.Id == id);
            AssignFailureReason? reason =
                shift is null ? AssignFailureReason.NotFound
                : dto.OnlyOpen && (shift.Status != ShiftStatus.Open || shift.EventArchived || shift.StartUtc <= now) ? AssignFailureReason.NotOpen
                : alreadyMine.Contains(id) ? AssignFailureReason.AlreadyAssigned
                : activeCounts.GetValueOrDefault(id) >= shift.Capacity ? AssignFailureReason.Full
                : null;

            if (reason is not null)
            {
                result.Failures.Add(new AssignFailureDto { ShiftId = id, Reason = reason.Value });
            }
        }

        if (result.Failures.Count > 0)
        {
            // Alt eller intet: ingen af vagterne tildeles.
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        dbContext.Assignments.AddRange(ids.Select(dto.ToEntity));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            // Sikkerhedsnet: det delvist unikke index (én aktiv tildeling pr. bruger og vagt) slog til.
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            result.Failures.AddRange(ids.Select(id => new AssignFailureDto { ShiftId = id, Reason = AssignFailureReason.AlreadyAssigned }));
            return result;
        }

        result.CreatedShiftIds = ids;
        return result;
    }

    public async Task<AssignmentDto?> CancelAsync(Guid assignmentId, Guid cancelledByUserId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Assignments.FirstOrDefaultAsync(x => x.Id == assignmentId && x.IsActive, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.IsActive = false;
        entity.CancelledAt = DateTime.UtcNow;
        entity.CancelledByUserId = cancelledByUserId;
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public Task<AssignmentDto?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default) =>
        dbContext.Assignments.AsNoTracking()
            .Where(x => x.Id == assignmentId)
            .Select(AssignmentMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<AssigneeDto>> GetActiveByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default) =>
        (from a in dbContext.Assignments.AsNoTracking()
         join u in dbContext.Users on a.UserId equals u.Id
         where a.ShiftId == shiftId && a.IsActive
         orderby a.CreatedAt
         select new AssigneeDto
         {
             AssignmentId = a.Id,
             UserId = u.Id,
             UserName = u.UserName,
             DisplayName = u.DisplayName,
             Source = a.Source,
             CreatedAt = a.CreatedAt
         }).ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, int>> GetActiveCountsAsync(IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        var ids = shiftIds.Distinct().ToList();
        return await dbContext.Assignments.AsNoTracking()
            .Where(a => ids.Contains(a.ShiftId) && a.IsActive)
            .GroupBy(a => a.ShiftId)
            .Select(g => new { ShiftId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ShiftId, x => x.Count, cancellationToken);
    }

    public Task<List<MyShiftDto>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        MyShifts(userId).OrderBy(x => x.StartUtc).ToListAsync(cancellationToken);

    public Task<MyShiftDto?> GetActiveForUserAndShiftAsync(Guid userId, Guid shiftId, CancellationToken cancellationToken = default) =>
        MyShifts(userId).Where(x => x.ShiftId == shiftId).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Assignments.AnyAsync(x => x.UserId == userId, cancellationToken);

    public Task<List<UpcomingAssignmentDto>> GetActiveStartingBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default) =>
        Upcoming().Where(x => x.StartUtc > fromUtc && x.StartUtc <= toUtc).OrderBy(x => x.StartUtc).ToListAsync(cancellationToken);

    public Task<UpcomingAssignmentDto?> GetActiveUpcomingByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default) =>
        Upcoming().Where(x => x.AssignmentId == assignmentId).FirstOrDefaultAsync(cancellationToken);

    private IQueryable<UpcomingAssignmentDto> Upcoming() =>
        from a in dbContext.Assignments.AsNoTracking()
        join s in dbContext.Shifts on a.ShiftId equals s.Id
        join e in dbContext.Events on s.EventId equals e.Id
        join u in dbContext.Users on a.UserId equals u.Id
        where a.IsActive && u.IsActive
        select new UpcomingAssignmentDto
        {
            AssignmentId = a.Id,
            UserId = a.UserId,
            ShiftId = s.Id,
            Title = s.Title,
            StartUtc = s.StartUtc,
            EndUtc = s.EndUtc,
            EventName = e.Name,
            EventLocation = e.Location,
            AssignedAt = a.CreatedAt
        };

    // Kun brugerens egne aktive tildelinger. Kladder er med, fordi brugeren må se sine egne tildelte vagter (krav K09/K11).
    private IQueryable<MyShiftDto> MyShifts(Guid userId) =>
        from a in dbContext.Assignments.AsNoTracking()
        join s in dbContext.Shifts on a.ShiftId equals s.Id
        join e in dbContext.Events on s.EventId equals e.Id
        where a.UserId == userId && a.IsActive
        select new MyShiftDto
        {
            AssignmentId = a.Id,
            ShiftId = s.Id,
            Title = s.Title,
            Description = s.Description,
            StartUtc = s.StartUtc,
            EndUtc = s.EndUtc,
            ShiftStatus = s.Status,
            EventId = e.Id,
            EventName = e.Name,
            EventLocation = e.Location,
            Source = a.Source,
            AssignedAt = a.CreatedAt
        };

    public async Task<List<AssignmentActivityDto>> GetRecentActivityAsync(int take, CancellationToken cancellationToken = default)
    {
        var created = await (from a in dbContext.Assignments.AsNoTracking()
                             join s in dbContext.Shifts on a.ShiftId equals s.Id
                             join e in dbContext.Events on s.EventId equals e.Id
                             join u in dbContext.Users on a.UserId equals u.Id
                             orderby a.CreatedAt descending
                             select new AssignmentActivityDto
                             {
                                 Kind = a.Source == AssignmentSource.Self ? AssignmentActivityKind.Enrolled : AssignmentActivityKind.Assigned,
                                 At = a.CreatedAt,
                                 UserDisplayName = u.DisplayName,
                                 ShiftId = s.Id,
                                 ShiftTitle = s.Title,
                                 EventId = e.Id,
                                 EventName = e.Name
                             }).Take(take).ToListAsync(cancellationToken);

        var removed = await (from a in dbContext.Assignments.AsNoTracking()
                             join s in dbContext.Shifts on a.ShiftId equals s.Id
                             join e in dbContext.Events on s.EventId equals e.Id
                             join u in dbContext.Users on a.UserId equals u.Id
                             where a.CancelledAt != null
                             orderby a.CancelledAt descending
                             select new AssignmentActivityDto
                             {
                                 Kind = AssignmentActivityKind.Removed,
                                 At = a.CancelledAt!.Value,
                                 UserDisplayName = u.DisplayName,
                                 ShiftId = s.Id,
                                 ShiftTitle = s.Title,
                                 EventId = e.Id,
                                 EventName = e.Name
                             }).Take(take).ToListAsync(cancellationToken);

        return created.Concat(removed).OrderByDescending(x => x.At).Take(take).ToList();
    }

    public Task<List<EventParticipantDto>> GetActiveForEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        (from a in dbContext.Assignments.AsNoTracking()
         join s in dbContext.Shifts on a.ShiftId equals s.Id
         join u in dbContext.Users on a.UserId equals u.Id
         where s.EventId == eventId && a.IsActive
         orderby s.StartUtc, s.Title, u.DisplayName
         select new EventParticipantDto
         {
             ShiftId = s.Id,
             ShiftTitle = s.Title,
             StartUtc = s.StartUtc,
             EndUtc = s.EndUtc,
             UserId = u.Id,
             DisplayName = u.DisplayName,
             UserName = u.UserName,
             Source = a.Source
         }).ToListAsync(cancellationToken);

    public Task<List<AssignmentHistoryDto>> GetHistoryForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (from a in dbContext.Assignments.AsNoTracking()
         join s in dbContext.Shifts on a.ShiftId equals s.Id
         join e in dbContext.Events on s.EventId equals e.Id
         where a.UserId == userId
         orderby s.StartUtc descending
         select new AssignmentHistoryDto
         {
             EventName = e.Name,
             ShiftTitle = s.Title,
             StartUtc = s.StartUtc,
             EndUtc = s.EndUtc,
             Source = a.Source,
             IsActive = a.IsActive,
             AssignedAt = a.CreatedAt,
             CancelledAt = a.CancelledAt
         }).ToListAsync(cancellationToken);
}
