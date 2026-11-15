# Fejlhåndtering (ProblemDetails og fejlsider)

**Gælder for:** alle fejlsvar i alle projekter.
**Kode:** `infrastructure/ErrorHandling/GlobalExceptionHandler.cs`, opsætningen i `Program.cs` (se `program.md`) og i web-delen `HomeController.Error`/`Status` (se `views.md`).
**Se også:** `controllers.md`, `services.md` (exceptions med statuskode), `managers.md` (forventede udfald), `authentication.md` (401/403).

## Princip

| Del | Fejlsvar |
|---|---|
| API-delen | Altid RFC 9110 `ProblemDetails` (`application/problem+json` med `type`, `title`, `status`, `detail?`, `traceId` …): valideringsfejl, `NotFound()`/`BadRequest()`/`Conflict()`, ukendte ruter, 401/403 og uhåndterede exceptions. |
| Web-delen | Uhåndterede exceptions viser fejlsiden (`/Home/Error`). Tomme 4xx/5xx-svar (fx `NotFound()` eller en ukendt side) viser statussiden (`/Home/Status/{kode}`). Valideringsfejl vises i formularen via `ModelState`. Manglende login giver login-siden. |
| Kombineret | Begge dele, valgt ud fra stien: `/api` får ProblemDetails, alt andet får sider (`ApiPath`). |

Det er sat op centralt og ikke pr. endpoint.

## Sådan er det sat op

| Del | Hvad den gør |
|---|---|
| `builder.Services.AddProblemDetails()` | ProblemDetails-body på 4xx/5xx fra ASP.NET Core's egne konventioner: `[ApiController]`'s automatiske 400 og `NotFound()`, `BadRequest()` osv. i API-controllere. |
| `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` + `UseExceptionHandler` som **første** middleware | Fanger alle uhåndterede exceptions og er det **eneste** sted, de logges: som `Error` ved 5xx og som `Warning` ved 4xx. I API-delen skriver den ProblemDetails. I web-delen returnerer den `false`, og middlewaren viser `/Home/Error`. |
| `SuppressDiagnosticsCallback = _ => true` (web-delen) | Når handleren returnerer `false`, ville middlewaren ellers logge den samme exception en gang til. |
| `UseStatusCodePages()` (API-delen) | Tomme 4xx/5xx-svar, fx en ukendt rute under API'et, får også en ProblemDetails-body. |
| `UseStatusCodePagesWithReExecute("/Home/Status/{0}")` (web-delen) | Tomme 4xx/5xx-svar på websider viser statussiden. Kræver `app.UseRouting()` eksplicit (se `program.md`). |
| `ApiKeyAuthenticationHandler.HandleChallengeAsync`/`HandleForbiddenAsync` (API-delen) | Skriver 401/403 som ProblemDetails via `IProblemDetailsService`. Basisklassen ville ellers sende en tom body. |

**Statuskode vs. detail:** Servicers exceptions med `HttpStatusCode` (`<System>Exception`) giver deres egen statuskode med `exception.Message` som `detail`. Alt andet giver 500, og `detail` kommer kun med i Development, så interne detaljer aldrig lækker i Production. Fejlsiden i web-delen viser aldrig exception-teksten, kun et fejl-id.

## Regler

1. Skriv fejl som normalt: `return NotFound();`, `return Conflict();` osv. Der skal ikke laves en body eller en side i hånden.
2. Ingen try/catch i controllere for at forme et fejlsvar eller for at logge. `GlobalExceptionHandler` gør begge dele.
3. Forventede udfald (ikke fundet, findes allerede …) kommer tilbage som `null`/`false`/enum fra repositories og managers og oversættes i controlleren: API-controllere til `NotFound()`/`Conflict()` osv., web-controllere til `NotFound()` eller `ModelState.AddModelError(...)` + viewet igen.
4. Alle services, også light pattern, har deres egen `<System>Exception` med `HttpStatusCode` (se `services.md` regel 14-15), og der er én `case` til hver i `GlobalExceptionHandler`. Fang den aldrig lokalt. Uden den bliver fejl i eksterne systemer til en anonym 500.
5. Lav aldrig egne fejl-DTO'er eller andre fejlformater i API-delen.
6. Fjernes en service, så fjern også dens `case` i `GlobalExceptionHandler`.
7. Med `UseExceptionHandler("/Home/Error")` er `Request.Path` allerede skrevet om til fejlsiden, når handleren kaldes. Den oprindelige sti hentes derfor fra `IExceptionHandlerPathFeature`, både til loggen og til valget mellem API og web.

## Skabelon: `GlobalExceptionHandler`

```csharp
using <projekt>.Infrastructure.Routing;   // [Kombineret]
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace <projekt>.Infrastructure.ErrorHandling;

public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, detail) = exception switch
        {
            // Én linje pr. service med egen exception:
            <System>Exception ex => ((int)ex.StatusCode, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, environment.IsDevelopment() ? exception.Message : null)
        };

        // I web-delen er Request.Path allerede skrevet om til /Home/Error. Den oprindelige sti ligger i featuren.
        var path = httpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path ?? httpContext.Request.Path.Value;

        logger.Log(statusCode >= StatusCodes.Status500InternalServerError ? LogLevel.Error : LogLevel.Warning,
            exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, path);

        httpContext.Response.StatusCode = statusCode;

        // [Kun web] Websider får altid fejlsiden fra UseExceptionHandler.
        return false;

        // [Kombineret] Websider får fejlsiden, /api får ProblemDetails.
        if (!ApiPath.IsApiPath(path))
        {
            return false;
        }

        // [API-del]
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = statusCode, Detail = detail }
        });
    }
}
```

Har projektet ingen services med egen exception, så består `switch`'en kun af `_`-grenen. I et rent web-projekt bruges `problemDetailsService` ikke og kan fjernes fra constructoren. Projektets aktuelle `case`s står i `<projekt>/.claude/CLAUDE.md` under Services.

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
