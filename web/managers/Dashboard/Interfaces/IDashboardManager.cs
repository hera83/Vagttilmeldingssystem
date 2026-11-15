using web.Managers.Dashboard.Dtos;

namespace web.Managers.Dashboard.Interfaces;

public interface IDashboardManager
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken = default);
}
