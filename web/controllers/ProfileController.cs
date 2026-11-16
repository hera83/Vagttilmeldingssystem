using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.Members.Interfaces;
using web.Repositories.Profiles.Interfaces;
using web.ViewModels.Profile;

namespace web.Controllers;

/// <summary>"Min profil": kun den indloggede brugers egen profil. Der er bevidst intet id i URL'en.</summary>
public class ProfileController(IProfileRepository profileRepository, IMemberManager memberManager) : Controller
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
            model.EmailNotifications = await profileRepository.GetEmailNotificationsAsync(userId, cancellationToken);
            return View(model);
        }

        await profileRepository.SaveAsync(userId, model.ToSaveProfileDto(), cancellationToken);
        TempData["Success"] = "Din profil er gemt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Notifications(NotificationsProfileViewModel model, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        await profileRepository.SetEmailNotificationsAsync(userId, model.EmailNotifications, cancellationToken);
        TempData["Success"] = model.EmailNotifications ? "E-mail-beskeder er slået til." : "E-mail-beskeder er slået fra.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>"Hent mine data" (GDPR art. 15): alt om den indloggede bruger som JSON-fil.</summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        var export = await memberManager.ExportMemberAsync(userId, cancellationToken);
        return export is null
            ? NotFound()
            : File(export.ToExportProfileViewModel().ToJsonBytes(), "application/json", export.ToExportFileName(own: true));
    }
}
