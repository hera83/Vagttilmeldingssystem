using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Repositories.Profiles.Interfaces;
using web.ViewModels.Profile;

namespace web.Controllers;

/// <summary>"Min profil": kun den indloggede brugers egen profil. Der er bevidst intet id i URL'en.</summary>
public class ProfileController(IProfileRepository profileRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        return View(profile.ToIndexProfileViewModel(User.Identity?.Name ?? string.Empty));
    }

    [HttpPost]
    public async Task<IActionResult> Index(IndexProfileViewModel model, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            model.FillDisplayFields(User.Identity?.Name ?? string.Empty, isComplete: false);
            return View(model);
        }

        await profileRepository.SaveAsync(userId, model.ToSaveProfileDto(), cancellationToken);
        TempData["Success"] = "Din profil er gemt.";
        return RedirectToAction(nameof(Index));
    }
}
