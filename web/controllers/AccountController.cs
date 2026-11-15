using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Repositories.Users.Interfaces;
using web.ViewModels.Account;

namespace web.Controllers;

public class AccountController(IUserRepository userRepository) : Controller
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
}
