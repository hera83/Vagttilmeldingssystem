using web.Data.Models;
using web.Repositories.Shifts.Dtos;

namespace web.Repositories.Shifts.Interfaces;

public interface IShiftRepository
{
    Task<ShiftDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Kun til admin: begivenhedens vagter i alle statusser (eller kun én status), sorteret efter starttid.</summary>
    Task<List<ShiftDto>> GetByEventIdAsync(Guid eventId, ShiftStatus? status = null, CancellationToken cancellationToken = default);

    Task<bool> AnyForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<ShiftDto> CreateAsync(Guid eventId, CreateShiftDto dto, CancellationToken cancellationToken = default);

    Task<ShiftDto?> UpdateAsync(Guid id, UpdateShiftDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
