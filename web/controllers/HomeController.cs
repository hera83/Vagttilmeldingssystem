using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.Enrollment.Interfaces;
using web.ViewModels.Home;

namespace web.Controllers;

public class HomeController(IEnrollmentManager enrollmentManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        var overview = await enrollmentManager.GetHomeAsync(userId, cancellationToken);
        return View(overview.ToIndexHomeViewModel(DateTime.UtcNow));
    }

    // Installationsvejledningen er anonym, så den kan nås fra login-siden. Den viser ingen data.
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Install() => View();

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorHomeViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult Status(int id) => View(new StatusHomeViewModel { StatusCode = id });
}
