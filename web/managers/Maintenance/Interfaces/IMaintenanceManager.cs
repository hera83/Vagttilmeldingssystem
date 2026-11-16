using web.Managers.Maintenance.Dtos;

namespace web.Managers.Maintenance.Interfaces;

public interface IMaintenanceManager
{
    /// <summary>Sletter færdige beskeder ældre end DataRetention:NotificationDays (90) og udløbne/brugte links til adgangskode.</summary>
    Task<CleanupResultDto> CleanupAsync(CancellationToken cancellationToken = default);
}
