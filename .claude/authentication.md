# Authentication og adgang (overblik)

**Gælder for:** alle projekter. Hvilket login der bruges hvor, fælles policies og opsætningen i kombinerede projekter.
**Kode (standardkomponenter):** `infrastructure/Authentication/AuthorizationPolicies.cs` (alle), `infrastructure/Authentication/CombinedAuthentication.cs` og `infrastructure/Routing/ApiPath.cs` (kombineret).
**Detaljer:** `apikeys.md` (`x-api-key` i API-delen) og `cookieauth.md` (cookie-login i web-delen).
**Se også:** `program.md` (registreringerne), `controllers.md` (attributter), `swagger.md` (Authorize-knappen), `errorhandling.md` (401/403).

## Hvilket login hvor

| Type | Login | Default scheme i `AddAuthentication` |
|---|---|---|
| API | `x-api-key` (se `apikeys.md`) | `ApiKeyAuthenticationOptions.Scheme` (`"ApiKey"`) |
| Web | Cookie (se `cookieauth.md`) | `CookieAuthenticationDefaults.AuthenticationScheme` (`"Cookies"`) |
| Kombineret | `x-api-key` under `/api`, cookie alle andre steder | `CombinedAuthentication.Scheme` (`"ApiKeyOrCookie"`), et policy-scheme, der vælger ud fra stien |

- **Alt kræver adgang som standard.** `AuthorizationOptions.FallbackPolicy` (`RequireAuthenticatedUser`) i `Program.cs` gælder alle endpoints og sider uden egen attribut. Almindelige controllere skal derfor **ikke** have `[Authorize]`.
- Uden gyldig adgang svarer API-delen 401/403 som ProblemDetails, og web-delen sender brugeren til `/Account/Login` eller `/Account/AccessDenied`.

## Kombinerede projekter

`CombinedAuthentication.SelectScheme` sender alle requests under `/api` til `x-api-key`-handleren og alle andre til cookie-handleren. Det giver:

- En cookie logger aldrig ind på `/api`, og en API-nøgle logger aldrig ind på en webside.
- Fallback-policyen og alle policies virker uden `AuthenticationSchemes` på attributterne.
- `/api`-kald får 401/403 som ProblemDetails, og websider sender videre til login-siden.

`ApiPath` er det eneste sted, der ved, at API'et ligger under `/api`. Fejlhåndtering, statussider og authentication bruger den alle sammen.

```csharp
// infrastructure/Routing/ApiPath.cs
namespace <projekt>.Infrastructure.Routing;

public static class ApiPath
{
    public const string Prefix = "/api";

    public static bool IsApiRequest(HttpContext context) => IsApiPath(context.Request.Path);

    public static bool IsApiPath(PathString path) => path.StartsWithSegments(Prefix);
}

// infrastructure/Authentication/CombinedAuthentication.cs
using <projekt>.Infrastructure.Routing;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace <projekt>.Infrastructure.Authentication;

public static class CombinedAuthentication
{
    public const string Scheme = "ApiKeyOrCookie";

    public static string SelectScheme(HttpContext context) =>
        ApiPath.IsApiRequest(context) ? ApiKeyAuthenticationOptions.Scheme : CookieAuthenticationDefaults.AuthenticationScheme;
}
```

API-controllerne i et kombineret projekt har route-præfikset `api/` og `[IgnoreAntiforgeryToken]` (se `controllers.md`).

## Policies

```csharp
// infrastructure/Authentication/AuthorizationPolicies.cs
namespace <projekt>.Infrastructure.Authentication;

public static class AuthorizationPolicies
{
    public const string MasterKeyOnly = "MasterKeyOnly";   // [API-del] kun master-nøglen
    public const string AdminOnly = "AdminOnly";           // [Web-del] kun brugere med IsAdmin
}
```

Tag kun de konstanter med, som projektets type bruger. De registreres i `AddAuthorization(...)` i `Program.cs` (se `program.md`).

## Regler for nye endpoints og sider

1. **Almindelige endpoints og sider:** ingen attribut. Fallback-policyen kræver login eller nøgle.
2. **Admin-funktioner:** `[Authorize(Policy = AuthorizationPolicies.MasterKeyOnly)]` på en API-controller og `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]` på en web-controller, på klassen. Brug altid disse policies til admin-funktioner og aldrig en hjemmelavet kontrol.
3. **Offentlige endpoints og sider:** `[AllowAnonymous]`, kun bevidst og kun hvor det er nødvendigt. Skriv begrundelsen i `<projekt>/.claude/CLAUDE.md` under Controllers og endpoints.
4. **Nye policies:** tilføj navnet som konstant i `AuthorizationPolicies`, og registrér policyen i `AddAuthorization(...)` i `Program.cs`.
5. Skal en action vide, hvem der kalder, så læs claims fra `User`: `ApiKeyClaimTypes.KeyType`/`KeyId` i API-delen (se `apikeys.md`) og `UserPrincipalFactory.GetUserId(User)` i web-delen (se `cookieauth.md`).

## Standard-adgang

| Endpoint / side | Findes i | Adgang | Hvorfor |
|---|---|---|---|
| `HealthController` (`GET /Health/Get`, kombineret `/api/Health/Get`) | API-delen | `[AllowAnonymous]` | Health check. |
| OpenAPI-dokumentet | API-delen | `.AllowAnonymous()` i `Program.cs` | Swagger UI skal kunne hente det uden nøgle. |
| Swagger UI og redirecten dertil | API-delen | Middleware uden for routing | Rammer aldrig authorization. |
| `KeysController` | API-delen | `MasterKeyOnly` | CRUD på API-nøgler + rollover. |
| `LogsController` | API-delen | `MasterKeyOnly` | Søgning i logs. |
| `AccountController.Login`/`AccessDenied` | Web-delen | `[AllowAnonymous]` | Skal kunne nås uden login. |
| `AccountController.Logout`/`ChangePassword` | Web-delen | Login | Egen konto. |
| `UsersController` | Web-delen | `AdminOnly` | Brugeradministration. |
| `HomeController.Error`/`Status` | Web-delen | `[AllowAnonymous]` | Fejl- og statussider skal også vises for brugere, der ikke er logget ind. |
| Statiske filer i `wwwroot/` | Web-delen | `MapStaticAssets().AllowAnonymous()` | Login-siden skal have sit css. |

Projektets øvrige endpoints og sider står i `<projekt>/.claude/CLAUDE.md`.

## Tjekliste: login i et nyt projekt

1. API-delen: alt fra tjeklisten i `apikeys.md`.
2. Web-delen: alt fra tjeklisten i `cookieauth.md`.
3. Kombineret: `ApiPath`, `CombinedAuthentication` og `[Kombineret]`-blokken i `Program.cs`.
4. Start projektet, og tjek:
   - API-delen: et kald uden nøgle giver 401 som ProblemDetails, en standard-nøgle giver 403 på `Keys/GetAll`, og master-nøglen giver 200.
   - Web-delen: `/` sender videre til login, login med `Users:InitialAdmin` virker, og en bruger uden admin får "Ingen adgang" på `/Users`.
   - Kombineret: en cookie alene giver 401 på `/api/...`, og en API-nøgle alene sender videre til login på websider.

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
