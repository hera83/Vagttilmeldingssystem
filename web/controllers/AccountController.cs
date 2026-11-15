using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Data.Models;
using web.Infrastructure.Authentication;
using web.Managers.AccountRecovery.Dtos;
using web.Managers.AccountRecovery.Interfaces;
using web.Repositories.Users.Interfaces;
using web.ViewModels.Account;

namespace web.Controllers;

public class AccountController(IUserRepository userRepository, IAccountRecoveryManager accountRecoveryManager) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        return View(new LoginAccountViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Password = string.Empty;
            return View(model);
        }

        var user = await userRepository.ValidateCredentialsAsync(model.UserName, model.Password, cancellationToken);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Forkert brugernavn eller adgangskode.");
            model.Password = string.Empty;
            return View(model);
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserPrincipalFactory.Create(user),
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl) : RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordAccountViewModel());

    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(new ChangePasswordAccountViewModel());
        }

        var userId = UserPrincipalFactory.GetUserId(User);
        var user = userId is null ? null : await userRepository.ChangePasswordAsync(userId.Value, model.CurrentPassword, model.NewPassword, cancellationToken);
        if (user is null)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Den nuværende adgangskode er forkert.");
            return View(new ChangePasswordAccountViewModel());
        }

        // Ny security stamp: log ind igen, så cookien stadig er gyldig.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserPrincipalFactory.Create(user));
        TempData["Success"] = "Din adgangskode er ændret.";
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    // Glemt adgangskode og invitation: anonyme, fordi brugeren ikke kan logge ind endnu.

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordAccountViewModel());

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await accountRecoveryManager.RequestPasswordResetAsync(model.Email, cancellationToken);

        // Samme svar, uanset om e-mailen findes (afslører ikke, hvem der er bruger).
        TempData["Success"] = "Hvis e-mailen findes i systemet, har vi sendt et link til at vælge en ny adgangskode. Linket gælder i 1 time. Tjek også spam-mappen.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> SetPassword(string? token, CancellationToken cancellationToken) =>
        View(new SetPasswordAccountViewModel
        {
            Token = token ?? string.Empty,
            LinkProblem = (await accountRecoveryManager.CheckLinkAsync(token ?? string.Empty, PasswordTokenPurpose.Invitation, cancellationToken)).ToLinkProblem()
        });

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SetPassword(SetPasswordAccountViewModel model, CancellationToken cancellationToken) =>
        await SetPasswordWithLinkAsync(model, PasswordTokenPurpose.Invitation, "Din adgangskode er gemt. Log ind med din e-mail og den nye adgangskode.", cancellationToken);

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> ResetPassword(string? token, CancellationToken cancellationToken) =>
        View(new ResetPasswordAccountViewModel
        {
            Token = token ?? string.Empty,
            LinkProblem = (await accountRecoveryManager.CheckLinkAsync(token ?? string.Empty, PasswordTokenPurpose.Reset, cancellationToken)).ToLinkProblem()
        });

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordAccountViewModel model, CancellationToken cancellationToken) =>
        await SetPasswordWithLinkAsync(model, PasswordTokenPurpose.Reset, "Din nye adgangskode er gemt, og du er logget ud alle andre steder. Log ind med den nye adgangskode.", cancellationToken);

    private async Task<IActionResult> SetPasswordWithLinkAsync(PasswordLinkFormViewModel model, PasswordTokenPurpose purpose, string successMessage, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.LinkProblem = (await accountRecoveryManager.CheckLinkAsync(model.Token, purpose, cancellationToken)).ToLinkProblem();
            model.NewPassword = model.ConfirmPassword = string.Empty;
            return View(model);
        }

        var status = await accountRecoveryManager.SetPasswordAsync(model.Token, purpose, model.NewPassword, cancellationToken);
        if (status != PasswordLinkStatus.Valid)
        {
            model.LinkProblem = status.ToLinkProblem();
            model.NewPassword = model.ConfirmPassword = string.Empty;
            return View(model);
        }

        // Er en anden bruger logget ind i browseren, logges den ud, så den rigtige bruger kan logge ind.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = successMessage;
        return RedirectToAction(nameof(Login));
    }
}
