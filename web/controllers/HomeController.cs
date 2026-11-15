using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Repositories.Profiles.Interfaces;
using web.ViewModels.Home;

namespace web.Controllers;

public class HomeController(IProfileRepository profileRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = UserPrincipalFactory.GetUserId(User);
        var profileComplete = userId is not null && await profileRepository.IsCompleteAsync(userId.Value, cancellationToken);
        return View(HomeViewModelMapper.ToIndexHomeViewModel(profileComplete));
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorHomeViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult Status(int id) => View(new StatusHomeViewModel { StatusCode = id });
}
