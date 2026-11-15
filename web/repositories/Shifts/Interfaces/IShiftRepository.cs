using web.Data.Models;
using web.Repositories.Shifts.Dtos;

namespace web.Repositories.Shifts.Interfaces;

public interface IShiftRepository
{
    Task<ShiftDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kun til admin: begivenhedens vagter i alle statusser (eller kun én status), sorteret efter starttid.</summary>
    Task<List<ShiftDto>> GetByEventIdAsync(Guid eventId, ShiftStatus? status = null, CancellationToken cancellationToken = default);

    Task<bool> AnyForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Vagt-id → begivenheds-id for de vagter, der findes.</summary>
    Task<Dictionary<Guid, Guid>> GetEventIdsAsync(IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Til forældrene: kun åbne vagter i ikke-arkiverede begivenheder, der ikke er startet endnu, sorteret efter starttid.
    /// Filtreringen sker i selve forespørgslen, så kladder aldrig forlader databasen (krav K10).
    /// </summary>
    Task<List<OpenShiftDto>> GetOpenAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Frigivelser, der endnu ikke er sendt beskeder om (åbne vagter med OpenNotifiedAt = null), grupperet pr. begivenhed og OpenedAt.
    /// Kladder er aldrig med (krav K10).
    /// </summary>
    Task<List<OpenedBatchDto>> GetUnnotifiedOpenedAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Markerer, at der er lagt beskeder i køen om vagterne. Returnerer antal opdaterede.</summary>
    Task<int> MarkOpenNotifiedAsync(IReadOnlyCollection<Guid> shiftIds, DateTime atUtc, CancellationToken cancellationToken = default);

    /// <summary>Antal kladder og åbne vagter pr. begivenhed (begivenheder uden vagter er ikke med).</summary>
    Task<List<EventShiftCountsDto>> GetStatusCountsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Frigivelsen: sætter de valgte kladder i begivenheden til Open i én transaktion og returnerer id'erne på dem,
    /// der blev åbnet. Vagter fra andre begivenheder og vagter, der allerede er åbne, ignoreres (idempotent).
    /// </summary>
    Task<List<Guid>> OpenAsync(Guid eventId, IReadOnlyCollection<Guid> shiftIds, Guid openedByUserId, CancellationToken cancellationToken = default);

    Task<ShiftDto> CreateAsync(Guid eventId, CreateShiftDto dto, CancellationToken cancellationToken = default);

    /// <summary>Retter vagten. Afvises med CapacityBelowTaken, hvis kapaciteten bliver lavere end antal aktive tildelinger (tjekket i samme transaktion).</summary>
    Task<ShiftChangeResultDto> UpdateAsync(Guid id, UpdateShiftDto dto, CancellationToken cancellationToken = default);

    /// <summary>Sletter vagten (og dens afmeldte historik). Afvises med HasActiveAssignments, hvis nogen er på vagten (tjekket i samme transaktion).</summary>
    Task<ShiftChangeResultDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Dashboardets tal for kommende vagter (ikke startet) i ikke-arkiverede begivenheder. Tæller aktive tildelinger.</summary>
    Task<ShiftOverviewDto> GetOverviewAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    /// <summary>Vagter, kladder, åbne, pladser og besatte pladser pr. begivenhed (begivenheder uden vagter er ikke med).</summary>
    Task<List<EventShiftStatsDto>> GetEventStatsAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default);

    /// <summary>De seneste frigivelser (pr. begivenhed og OpenedAt), nyeste først.</summary>
    Task<List<OpeningActivityDto>> GetRecentOpeningsAsync(int take, CancellationToken cancellationToken = default);
}
