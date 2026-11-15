# Cookie-login (web-delen)

**Gælder for:** kun web-delen (web-projekter og websiderne i kombinerede projekter).
**Kode (standardkomponenter):** `infrastructure/Authentication/` (`UserClaimTypes`, `UserPrincipalFactory`, `UserCookieEvents`), `repositories/Users/`, `data/Models/User.cs` + `data/Configurations/UserConfiguration.cs`, `controllers/AccountController.cs`, `controllers/UsersController.cs`, `viewmodels/Account/`, `viewmodels/Users/`, `views/Account/`, `views/Users/`.
**Se også:** `authentication.md` (overblik, policies, kombineret), `program.md` (`AddCookie` og første administrator), `views.md`, `controllers.md`.

## Sådan virker det

- Brugerne ligger i tabellen `Users` i `app.db`. Adgangskoder hashes med `PasswordHasher<User>` fra ASP.NET Core Identity (PBKDF2 med salt). Det kræver ingen ekstra pakke og ingen Identity-tabeller.
- `AccountController.Login` tjekker brugernavn og adgangskode via `IUserRepository.ValidateCredentialsAsync` og logger ind med `HttpContext.SignInAsync(...)`. Cookien `<projekt>.auth` er `HttpOnly` og `SameSite=Lax`. I Production er den altid `Secure`, i Development følger den requesten, så `http`-profilen også virker. Den udløber efter 8 timer uden aktivitet (`SlidingExpiration`).
- Claims: `NameIdentifier` (bruger-id), `Name` (brugernavn), `display_name`, `security_stamp` og rollen `Admin`, hvis brugeren er administrator.
- `UserCookieEvents.ValidatePrincipal` slår brugeren op ved hvert request og afviser cookien, hvis brugeren er slettet, deaktiveret, eller hvis `SecurityStamp` er ændret. Stampen skiftes, når adgangskoden ændres, eller når brugeren aktiveres/deaktiveres. Så slår ændringerne igennem med det samme og ikke først, når cookien udløber.
- Brugernavne er unikke uden forskel på store og små bogstaver (kolonnen har collation `NOCASE`).
- **Første administrator:** er tabellen `Users` tom ved opstart, og er `Users:InitialAdmin:UserName` og `:Password` sat, så opretter `Program.cs` en administrator med de værdier (se `program.md`). Bagefter bruges værdierne ikke, og de kan fjernes. Adgangskoden er en secret.
- Uden login sendes brugeren til `/Account/Login?ReturnUrl=…`. Uden rettighed sendes brugeren til `/Account/AccessDenied`.

## Regler

1. Adgangskoder gemmes kun som hash fra `PasswordHasher<User>`. De logges aldrig, vises aldrig og sendes aldrig retur i en ViewModel (`Password`-felter genudfyldes ikke, når en formular vises igen).
2. Nye adgangskoder skal være mindst 12 tegn (`[StringLength(200, MinimumLength = 12)]` på ViewModel'en).
3. Login-fejl får altid samme besked ("Forkert brugernavn eller adgangskode."), uanset om brugeren findes, er deaktiveret eller har skrevet forkert. Repositoriet verificerer mod et dummy-hash, når brugeren ikke findes, så svartiden ikke afslører det.
4. `ReturnUrl` følges kun, når `Url.IsLocalUrl(...)` er sand. Ellers sendes brugeren til forsiden.
5. Al ændring af adgangskode, aktiv-status eller sletning går gennem `IUserRepository`, som skifter `SecurityStamp`. Rør aldrig tabellen andre steder.
6. Efter brugerens egen ændring af adgangskode logges der ind igen med den nye stamp, ellers bliver brugeren logget ud ved næste request.
7. En administrator kan ikke slette eller deaktivere sig selv og kan ikke fjerne sin egen admin-rettighed. Så findes der altid mindst én aktiv administrator.
8. Rettigheder ud over "logget ind" og "administrator" laves som nye roller eller policies (se `authentication.md`) og står i projektfilen.

## Skabeloner: infrastructure

```csharp
// infrastructure/Authentication/UserClaimTypes.cs
namespace <projekt>.Infrastructure.Authentication;

public static class UserClaimTypes
{
    public const string DisplayName = "display_name";
    public const string SecurityStamp = "security_stamp";

    public const string AdminRole = "Admin";
}

// infrastructure/Authentication/UserPrincipalFactory.cs
using System.Security.Claims;
using <projekt>.Repositories.Users.Dtos;
using Microsoft.AspNetCore.Authentication.Cookies;

public static class UserPrincipalFactory
{
    public static ClaimsPrincipal Create(UserDto user)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(UserClaimTypes.DisplayName, user.DisplayName),
            new(UserClaimTypes.SecurityStamp, user.SecurityStamp.ToString())
        ];

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, UserClaimTypes.AdminRole));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

// infrastructure/Authentication/UserCookieEvents.cs
using System.Security.Claims;
using <projekt>.Repositories.Users.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

public class UserCookieEvents(IUserRepository userRepository) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userId = context.Principal is null ? null : UserPrincipalFactory.GetUserId(context.Principal);
        var stamp = context.Principal?.FindFirstValue(UserClaimTypes.SecurityStamp);
        var user = userId is null ? null : await userRepository.GetByIdAsync(userId.Value, context.HttpContext.RequestAborted);

        if (user is null || !user.IsActive || user.SecurityStamp.ToString() != stamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
```

`UserCookieEvents` registreres som scoped og kobles på med `options.EventsType = typeof(UserCookieEvents)` i `AddCookie` (se `program.md`).

## Skabeloner: tabel

```csharp
// data/Models/User.cs
public class User : EntityBase
{
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
}

// data/Configurations/UserConfiguration.cs
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // NOCASE: brugernavne er unikke og slås op uden forskel på store og små bogstaver.
        builder.Property(x => x.UserName).HasMaxLength(100).UseCollation("NOCASE");
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.PasswordHash).HasMaxLength(500);
        builder.HasIndex(x => x.UserName).IsUnique();
    }
}
```

`<Projekt>DbContext` får `public DbSet<User> Users => Set<User>();`.

## Repository: `repositories/Users/`

Bygges efter `repositories.md`. Dtos:

| Dto | Felter |
|---|---|
| `UserDto` | `Id`, `UserName`, `DisplayName`, `SecurityStamp`, `IsAdmin`, `IsActive`, `LastLoginAt?`, `CreatedAt`. Aldrig `PasswordHash`. |
| `CreateUserDto` | `UserName`, `DisplayName`, `Password` (klartekst, hashes i repositoriet), `IsAdmin` |
| `UpdateUserDto` | `DisplayName`, `IsAdmin`, `IsActive` |

| Metode | Beskrivelse |
|---|---|
| `AnyAsync()` | Findes der brugere? Bruges til første administrator. |
| `GetAllAsync()` | Alle brugere sorteret efter brugernavn. |
| `GetByIdAsync(Guid id)` | Én bruger, eller `null`. Bruges også af `UserCookieEvents` ved hvert request. |
| `ValidateCredentialsAsync(string userName, string password)` | Aktiv bruger med korrekt adgangskode, ellers `null`. Sætter `LastLoginAt` og opgraderer hashet, hvis `PasswordHasher` beder om det. |
| `CreateAsync(CreateUserDto dto)` | Opretter og returnerer brugeren, eller `null`, hvis brugernavnet findes. |
| `UpdateAsync(Guid id, UpdateUserDto dto)` | Opdaterer, eller `null`. Skifter `SecurityStamp`, hvis `IsActive` ændres. |
| `SetPasswordAsync(Guid id, string newPassword)` | Admin sætter ny adgangskode. Skifter `SecurityStamp`. `false`, hvis brugeren ikke findes. |
| `ChangePasswordAsync(Guid id, string currentPassword, string newPassword)` | Brugerens egen ændring. `null`, hvis den nuværende adgangskode er forkert. Skifter `SecurityStamp`. |
| `DeleteAsync(Guid id)` | `true`, hvis rækken fandtes. |

```csharp
// repositories/Users/UserRepository.cs
using <projekt>.Data;
using <projekt>.Data.Models;
using <projekt>.Repositories.Users.Dtos;
using <projekt>.Repositories.Users.Interfaces;
using <projekt>.Repositories.Users.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace <projekt>.Repositories.Users;

public class UserRepository(<Projekt>DbContext dbContext) : IUserRepository
{
    private static readonly PasswordHasher<User> Hasher = new();

    // Bruges, når brugernavnet ikke findes, så svartiden ikke afslører, om brugeren findes.
    private static readonly string DummyHash = Hasher.HashPassword(new User(), Guid.NewGuid().ToString());

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(cancellationToken);

    public Task<List<UserDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking()
            .OrderBy(x => x.UserName)
            .Select(UserMapper.ToDtoExpression)
            .ToListAsync(cancellationToken);

    public Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(UserMapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<UserDto?> ValidateCredentialsAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.UserName == userName.Trim(), cancellationToken);
        if (entity is null || !entity.IsActive)
        {
            Hasher.VerifyHashedPassword(new User(), DummyHash, password);
            return null;
        }

        var result = Hasher.VerifyHashedPassword(entity, entity.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            entity.PasswordHash = Hasher.HashPassword(entity, password);
        }

        entity.LastLoginAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<UserDto?> CreateAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Users.AnyAsync(x => x.UserName == dto.UserName.Trim(), cancellationToken))
        {
            return null;
        }

        var entity = dto.ToEntity();
        entity.PasswordHash = Hasher.HashPassword(entity, dto.Password);
        dbContext.Users.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<UserDto?> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        dto.ApplyTo(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<bool> SetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.PasswordHash = Hasher.HashPassword(entity, newPassword);
        entity.SecurityStamp = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserDto?> ChangePasswordAsync(Guid id, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null || Hasher.VerifyHashedPassword(entity, entity.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
        {
            return null;
        }

        entity.PasswordHash = Hasher.HashPassword(entity, newPassword);
        entity.SecurityStamp = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Users.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}

// repositories/Users/Mappers/UserMapper.cs (ud over ToDtoExpression/ToDto fra repositories.md)
public static User ToEntity(this CreateUserDto dto) => new()
{
    // PasswordHash sættes af repositoriet, som ejer hashingen.
    UserName = dto.UserName.Trim(),
    DisplayName = dto.DisplayName.Trim(),
    IsAdmin = dto.IsAdmin,
    IsActive = true
};

public static void ApplyTo(this UpdateUserDto dto, User entity)
{
    entity.DisplayName = dto.DisplayName.Trim();
    entity.IsAdmin = dto.IsAdmin;
    if (entity.IsActive != dto.IsActive)
    {
        entity.IsActive = dto.IsActive;
        entity.SecurityStamp = Guid.NewGuid();
    }
}
```

## Skabelon: `AccountController`

```csharp
// controllers/AccountController.cs
using <projekt>.Infrastructure.Authentication;
using <projekt>.Repositories.Users.Interfaces;
using <projekt>.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace <projekt>.Controllers;

public class AccountController(IUserRepository userRepository) : Controller
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginAccountViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginAccountViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userRepository.ValidateCredentialsAsync(model.UserName, model.Password, cancellationToken);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Forkert brugernavn eller adgangskode.");
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
            return View(model);
        }

        var userId = UserPrincipalFactory.GetUserId(User);
        var user = userId is null ? null : await userRepository.ChangePasswordAsync(userId.Value, model.CurrentPassword, model.NewPassword, cancellationToken);
        if (user is null)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Den nuværende adgangskode er forkert.");
            return View(model);
        }

        // Ny security stamp: log ind igen, så cookien stadig er gyldig.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, UserPrincipalFactory.Create(user));
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}

// viewmodels/Account/LoginAccountViewModel.cs
namespace <projekt>.ViewModels.Account;

public class LoginAccountViewModel
{
    [Required(ErrorMessage = "Skriv dit brugernavn.")]
    [Display(Name = "Brugernavn")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv din adgangskode.")]
    [DataType(DataType.Password)]
    [Display(Name = "Adgangskode")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Husk mig")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

// viewmodels/Account/ChangePasswordAccountViewModel.cs
public class ChangePasswordAccountViewModel
{
    [Required(ErrorMessage = "Skriv din nuværende adgangskode.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nuværende adgangskode")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv en ny adgangskode.")]
    [StringLength(200, MinimumLength = 12, ErrorMessage = "Adgangskoden skal være mindst {2} tegn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Ny adgangskode")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "De to adgangskoder er ikke ens.")]
    [DataType(DataType.Password)]
    [Display(Name = "Gentag ny adgangskode")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

```cshtml
@* views/Account/Login.cshtml *@
@model <projekt>.ViewModels.Account.LoginAccountViewModel
@{
    ViewData["Title"] = "Log ind";
}
<h1>Log ind</h1>
<form asp-action="Login" method="post">
    <div asp-validation-summary="ModelOnly"></div>
    <input asp-for="ReturnUrl" type="hidden" />

    <label asp-for="UserName"></label>
    <input asp-for="UserName" autocomplete="username" />
    <span asp-validation-for="UserName"></span>

    <label asp-for="Password"></label>
    <input asp-for="Password" autocomplete="current-password" />
    <span asp-validation-for="Password"></span>

    <label><input asp-for="RememberMe" /> @Html.DisplayNameFor(m => m.RememberMe)</label>

    <button type="submit">Log ind</button>
</form>
```

`AccessDenied.cshtml` er en simpel side uden model ("Du har ikke adgang til siden."). `ChangePassword.cshtml` følger formular-mønstret i `views.md` med `autocomplete="current-password"`/`"new-password"`.

## `UsersController`

Har `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]` på klassen og bygges efter web-skabelonen i `controllers.md` med ViewModels i `viewmodels/Users/` og views i `views/Users/`:

| Action | HTTP | ViewModel | Gør |
|---|---|---|---|
| `Index` | GET | `IndexUsersViewModel` (`Users`: liste af `UserListItemViewModel`) | Viser alle brugere. |
| `Create` | GET + POST | `CreateUsersViewModel` (`UserName`, `DisplayName`, `Password` mindst 12 tegn, `IsAdmin`) | Opretter. "Brugernavnet findes allerede." i `ModelState`, når `CreateAsync` returnerer `null`. |
| `Edit` | GET + POST `{id}` | `EditUsersViewModel` (`DisplayName`, `IsAdmin`, `IsActive`) | Retter. |
| `ResetPassword` | GET + POST `{id}` | `ResetPasswordUsersViewModel` (`NewPassword` mindst 12 tegn, `ConfirmPassword`) | Sætter ny adgangskode via `SetPasswordAsync`. |
| `Delete` | POST `{id}` | – | Sletter. |

Regel 7 håndhæves i `Edit` og `Delete`: er `id` den indloggede brugers eget (`UserPrincipalFactory.GetUserId(User)`), så afvis med en `ModelState`-fejl (Edit) eller `BadRequest()` (Delete).

## Tjekliste: cookie-login i et nyt projekt

1. Filerne i `infrastructure/Authentication/` ovenfor + `AuthorizationPolicies` (se `authentication.md`).
2. `User`-model og configuration, `DbSet` i `<Projekt>DbContext` og en migration.
3. `repositories/Users/` med alle fire undermapper og `Documents/Users.md`.
4. `AccountController` og `UsersController` med ViewModels og views.
5. `Users:InitialAdmin:UserName` og `:Password` i `appsettings.json` med tomme værdier og rigtige værdier i `appsettings.Development.json`.
6. `[Kun web]`- eller `[Kombineret]`-blokken for authentication, `UserCookieEvents`, `AdminOnly` og første administrator i `Program.cs` (se `program.md`).

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
