using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.Dashboard.Interfaces;
using web.ViewModels.Dashboard;

namespace web.Controllers;

/// <summary>Admin-dashboardet (mockup 02). Admins forside (/) sender hertil.</summary>
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class DashboardController(IDashboardManager dashboardManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View((await dashboardManager.GetAsync(cancellationToken)).ToIndexDashboardViewModel());
}
