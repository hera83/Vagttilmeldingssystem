using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Repositories.Users.Interfaces;
using web.ViewModels.Users;

namespace web.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class UsersController(IUserRepository userRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return View(users.ToIndexUsersViewModel(UserPrincipalFactory.GetUserId(User)));
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateUsersViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateUsersViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Password = string.Empty;
            return View(model);
        }

        var created = await userRepository.CreateAsync(model.ToCreateUserDto(), cancellationToken);
        if (created is null)
        {
            ModelState.AddModelError(nameof(model.UserName), "Der findes allerede en bruger med denne e-mail.");
            model.Password = string.Empty;
            return View(model);
        }

        TempData["Success"] = $"Brugeren {created.DisplayName} er oprettet.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return View(user.ToEditUsersViewModel(UserPrincipalFactory.GetUserId(User)));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, EditUsersViewModel model, CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        // En administrator kan ikke deaktivere sig selv eller fjerne sin egen admin-rettighed.
        var isCurrentUser = UserPrincipalFactory.GetUserId(User) == id;
        if (isCurrentUser && (!model.IsActive || !model.IsAdmin))
        {
            ModelState.AddModelError(string.Empty, "Du kan ikke deaktivere dig selv eller fjerne din egen admin-rettighed.");
        }

        if (!ModelState.IsValid)
        {
            model.UserName = existing.UserName;
            model.IsCurrentUser = isCurrentUser;
            return View(model);
        }

        var updated = await userRepository.UpdateAsync(id, model.ToUpdateUserDto(), cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        TempData["Success"] = $"Brugeren {updated.DisplayName} er gemt.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return View(user.ToResetPasswordUsersViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordUsersViewModel model, CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(existing.ToResetPasswordUsersViewModel());
        }

        await userRepository.SetPasswordAsync(id, model.NewPassword, cancellationToken);
        TempData["Success"] = $"Der er sat en ny adgangskode for {existing.DisplayName}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) == id)
        {
            return BadRequest();
        }

        if (!await userRepository.DeleteAsync(id, cancellationToken))
        {
            return NotFound();
        }

        TempData["Success"] = "Brugeren er slettet.";
        return RedirectToAction(nameof(Index));
    }
}
