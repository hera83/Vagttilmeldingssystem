using web.Repositories.Assignments.Dtos;

namespace web.Repositories.Assignments.Interfaces;

public interface IAssignmentRepository
{
    /// <summary>Bookingkernen: tildeler alle vagter i én transaktion, eller ingen. Kapaciteten kan aldrig overskrides.</summary>
    Task<AssignResultDto> CreateManyAsync(CreateAssignmentsDto dto, CancellationToken cancellationToken = default);

    /// <summary>Afmelder (soft cancel). null, hvis tildelingen ikke findes eller allerede er afmeldt.</summary>
    Task<AssignmentDto?> CancelAsync(Guid assignmentId, Guid cancelledByUserId, CancellationToken cancellationToken = default);

    Task<AssignmentDto?> GetByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    Task<List<AssigneeDto>> GetActiveByShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetActiveCountsAsync(IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default);

    Task<List<MyShiftDto>> GetActiveForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<MyShiftDto?> GetActiveForUserAndShiftAsync(Guid userId, Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>Har brugeren nogen tildelinger (også afmeldte)? Så kan brugeren ikke slettes.</summary>
    Task<bool> AnyForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktive tildelinger hos aktive brugere, hvor vagten starter i intervallet (fromUtc, toUtc]. Også tildelte kladder,
    /// fordi brugeren kan se dem i sin kalender. Bruges til påmindelser.
    /// </summary>
    Task<List<UpcomingAssignmentDto>> GetActiveStartingBetweenAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);

    /// <summary>Én aktiv tildeling hos en aktiv bruger med vagtens tidspunkt, eller null (afmeldt, slettet eller deaktiveret bruger).</summary>
    Task<UpcomingAssignmentDto?> GetActiveUpcomingByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    /// <summary>De seneste tilmeldinger, admin-tildelinger og fjernelser, nyeste først. Kun navne. Kun til admin.</summary>
    Task<List<AssignmentActivityDto>> GetRecentActivityAsync(int take, CancellationToken cancellationToken = default);

    /// <summary>Aktive tildelinger i en begivenhed med brugerens navn og e-mail (deltagerlisten). Kun til admin.</summary>
    Task<List<EventParticipantDto>> GetActiveForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Alle brugerens tildelinger, også afmeldte, nyeste vagt først (dataudtræk).</summary>
    Task<List<AssignmentHistoryDto>> GetHistoryForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
