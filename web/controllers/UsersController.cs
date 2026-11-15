using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.AccountRecovery.Dtos;
using web.Managers.AccountRecovery.Interfaces;
using web.Managers.Members.Dtos;
using web.Managers.Members.Interfaces;
using web.Repositories.Users.Dtos;
using web.Repositories.Users.Interfaces;
using web.ViewModels.Users;

namespace web.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class UsersController(IUserRepository userRepository, IMemberManager memberManager, IAccountRecoveryManager accountRecoveryManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var members = await memberManager.GetMembersAsync(cancellationToken);
        return View(members.ToIndexUsersViewModel(UserPrincipalFactory.GetUserId(User)));
    }

    [HttpGet]
    public async Task<IActionResult> Profile(Guid id, CancellationToken cancellationToken)
    {
        var member = await memberManager.GetMemberAsync(id, cancellationToken);
        if (member is null)
        {
            return NotFound();
        }

        return View(member.ToProfileUsersViewModel());
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateUsersViewModel { SendInvitation = true });

    [HttpPost]
    public async Task<IActionResult> Create(CreateUsersViewModel model, CancellationToken cancellationToken)
    {
        if (model.SendInvitation)
        {
            // Med invitation vælger brugeren selv sin adgangskode.
            ModelState.Remove(nameof(model.Password));
        }

        if (!ModelState.IsValid)
        {
            model.Password = string.Empty;
            return View(model);
        }

        if (model.SendInvitation)
        {
            var invited = await accountRecoveryManager.InviteNewUserAsync(model.ToInviteUserDto(), cancellationToken);
            switch (invited.Status)
            {
                case InviteUserStatus.Success:
                    TempData["Success"] = $"Brugeren {invited.User!.DisplayName} er oprettet, og der er sendt en invitation til {invited.User.UserName}.";
                    return RedirectToAction(nameof(Index));
                case InviteUserStatus.UserNameTaken:
                    ModelState.AddModelError(nameof(model.UserName), "Der findes allerede en bruger med denne e-mail.");
                    break;
                default:
                    ModelState.AddModelError(string.Empty, "Der kan ikke sendes e-mails, fordi e-mail (Mail) eller App:BaseUrl ikke er sat op. Fjern fluebenet ved Send invitation på e-mail, og skriv en adgangskode i stedet.");
                    break;
            }

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
        model.IsCurrentUser = isCurrentUser;
        if (isCurrentUser && (!model.IsActive || !model.IsAdmin))
        {
            ModelState.AddModelError(string.Empty, "Du kan ikke deaktivere dig selv eller fjerne din egen admin-rettighed.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // E-mailen (brugernavnet) ændres for sig, så "findes allerede" kan vises ved feltet.
        var userNameResult = await userRepository.ChangeUserNameAsync(id, model.UserName, cancellationToken);
        if (userNameResult == ChangeUserNameResult.Taken)
        {
            ModelState.AddModelError(nameof(model.UserName), "Der findes allerede en bruger med denne e-mail.");
            return View(model);
        }

        if (userNameResult == ChangeUserNameResult.NotFound)
        {
            return NotFound();
        }

        var updated = await userRepository.UpdateAsync(id, model.ToUpdateUserDto(), cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        // Ny e-mail på egen konto giver ny security stamp: log ind igen, så administratoren ikke bliver smidt ud.
        if (isCurrentUser && userNameResult == ChangeUserNameResult.Success)
        {
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserPrincipalFactory.Create(updated));
        }

        TempData["Success"] = userNameResult == ChangeUserNameResult.Success
            ? $"Brugeren {updated.DisplayName} er gemt. Brugeren skal fremover logge ind med {updated.UserName}."
            : $"Brugeren {updated.DisplayName} er gemt.";
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

        var result = await memberManager.DeleteMemberAsync(id, cancellationToken);
        if (result == DeleteMemberResult.NotFound)
        {
            return NotFound();
        }

        TempData[result == DeleteMemberResult.Deleted ? "Success" : "Error"] = result == DeleteMemberResult.Deleted
            ? "Brugeren er slettet."
            : "Brugeren har vagter og kan ikke slettes, så historikken bevares. Deaktivér brugeren i stedet under Ret.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Invite(Guid id, CancellationToken cancellationToken)
    {
        switch (await accountRecoveryManager.SendInvitationAsync(id, cancellationToken))
        {
            case SendInvitationResult.NotFound:
                return NotFound();
            case SendInvitationResult.Sent:
                TempData["Success"] = "Der er sendt en ny invitation. Tidligere invitationslinks virker ikke længere.";
                break;
            case SendInvitationResult.Inactive:
                TempData["Error"] = "Brugeren er deaktiveret. Gør brugeren aktiv under Ret først.";
                break;
            default:
                TempData["Error"] = "Der kan ikke sendes e-mails, fordi e-mail (Mail) eller App:BaseUrl ikke er sat op.";
                break;
        }

        return RedirectToAction(nameof(Index));
    }
}
