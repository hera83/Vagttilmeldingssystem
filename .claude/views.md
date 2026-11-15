# Views og statiske filer (`views/` + `wwwroot/`)

**Gælder for:** kun web-delen (web-projekter og websiderne i kombinerede projekter).
**Kode (standardkomponenter):** `views/_ViewImports.cshtml`, `views/_ViewStart.cshtml`, `views/Shared/_Layout.cshtml`, `views/Home/` (`Index`, `Error`, `Status`), `controllers/HomeController.cs`, `viewmodels/Home/`, `wwwroot/css/site.css`.
**Se også:** `controllers.md` (web-controllere og ViewModels), `cookieauth.md` (login-siderne), `errorhandling.md` (fejl- og statussider), `program.md` (view-lokationer og statiske filer).

## Struktur

```text
<projekt>/
├── views/                              ← små bogstaver (project.md)
│   ├── _ViewImports.cshtml             ← usings og tag helpers for alle views
│   ├── _ViewStart.cshtml               ← sætter Layout = "_Layout"
│   ├── Shared/
│   │   ├── _Layout.cshtml              ← fælles ramme: <head>, navigation, log ud
│   │   └── _<Navn>Partial.cshtml       ← partials, som flere views bruger
│   └── <Navn>/                         ← samme <Navn> som web-controlleren
│       └── <Action>.cshtml             ← ét view pr. action, der viser en side
└── wwwroot/                            ← statiske filer, serveres fra roden (/css/site.css)
    ├── css/site.css
    ├── js/                             ← egne scripts
    └── lib/                            ← tredjeparts-biblioteker, hvis projektet bruger nogen
```

## Regler

1. Views ligger i `views/<Navn>/<Action>.cshtml`. `views/` staves med små bogstaver, og derfor sætter `Program.cs` view-lokationerne til `/views/{1}/{0}.cshtml` og `/views/Shared/{0}.cshtml` (se `program.md`). Uden det finder Razor ikke views på Linux/Docker. Undermapperne (`Shared/`, `<Navn>/`) er PascalCase.
2. Hvert view har `@model <projekt>.ViewModels.<Navn>.<Action><Navn>ViewModel` øverst. Views uden data (fx `AccessDenied`) har ingen model.
3. Et view viser kun data fra sin ViewModel. Ingen dataadgang, ingen `@inject` af repositories, managers eller services, og ingen forretningslogik. Simple visningsvalg (`@if`, `@foreach`, formatering) er fint.
4. Brug altid tag helpers til links og formularer: `asp-controller`, `asp-action`, `asp-route-id`, `asp-for`, `asp-validation-for`, `asp-validation-summary`. Skriv aldrig URL'er i hånden. Så passer de altid til routen.
5. Formularer er altid `<form asp-action="…" method="post">`. Tag helperen indsætter anti-forgery-tokenet, som filteret i `Program.cs` kræver.
6. Sletning og andre ændringer er en POST-formular (en knap), aldrig et link.
7. Validering sker altid på serveren (`ModelState`). Validering i browseren (jQuery Validation i `wwwroot/lib/`) er valgfri og står i projektfilen, hvis den bruges.
8. Tekster i views skrives på projektets UI-sprog. Feltnavne kommer fra `[Display(Name = ...)]` på ViewModel'en (`<label asp-for>`), ikke fra faste tekster i viewet.
9. Sidetitlen sættes øverst i hvert view med `ViewData["Title"] = "…";` og vises af `_Layout`.
10. Statiske filer ligger i `wwwroot/` og refereres med `~/`: `<link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />`. `MapStaticAssets()` serverer dem anonymt, så login-siden også har sit css.
11. Hent ikke scripts eller css fra et CDN, medmindre projektfilen siger det. Læg filerne i `wwwroot/lib/`.
12. Datoer gemmes i UTC og omregnes kun til lokal tid i viewet, fx `@Model.CreatedAt.ToLocalTime().ToString("g")`.
13. `_Layout` viser kun menupunkter, brugeren har adgang til (fx `User.IsInRole(UserClaimTypes.AdminRole)` for admin-sider). Selve adgangen håndhæves stadig af controlleren.
14. Nyt view eller ny side: opdatér tabellen Controllers og endpoints i `<projekt>/.claude/CLAUDE.md`.

## `HomeController` (standard)

| Action | Adgang | Formål |
|---|---|---|
| `Index` | Login | Forsiden (`/`). |
| `Error` | `[AllowAnonymous]` + `[IgnoreAntiforgeryToken]` | Fejlsiden ved uhåndterede exceptions (`UseExceptionHandler("/Home/Error")`). Viser et fejl-id, aldrig exception-teksten. |
| `Status` | `[AllowAnonymous]` + `[IgnoreAntiforgeryToken]` | Statussiden ved tomme 4xx/5xx-svar, fx `NotFound()` (`UseStatusCodePagesWithReExecute("/Home/Status/{0}")`). |

`Error` og `Status` kører også, når den oprindelige request var en POST. De skal derfor have `[IgnoreAntiforgeryToken]`, ellers afviser anti-forgery-filteret fejlsiden selv, og brugeren får et tomt svar.

## Skabeloner

```csharp
// controllers/HomeController.cs
using System.Diagnostics;
using <projekt>.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace <projekt>.Controllers;

public class HomeController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorHomeViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public IActionResult Status(int id) => View(new StatusHomeViewModel { StatusCode = id });
}

// viewmodels/Home/ErrorHomeViewModel.cs
namespace <projekt>.ViewModels.Home;

public class ErrorHomeViewModel
{
    public string? RequestId { get; set; }
}

// viewmodels/Home/StatusHomeViewModel.cs
public class StatusHomeViewModel
{
    public int StatusCode { get; set; }
}
```

`Error` og `Status` har bevidst ingen `[HttpGet]`, fordi de skal svare på re-executede requests med alle metoder.

```cshtml
@* views/_ViewImports.cshtml *@
@using <projekt>
@using <projekt>.Infrastructure.Authentication
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

```cshtml
@* views/_ViewStart.cshtml *@
@{
    Layout = "_Layout";
}
```

```cshtml
@* views/Shared/_Layout.cshtml *@
<!DOCTYPE html>
<html lang="da">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] – <visningsnavn></title>
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
    <header>
        <nav>
            <a asp-controller="Home" asp-action="Index"><visningsnavn></a>
            @if (User.Identity?.IsAuthenticated == true)
            {
                @if (User.IsInRole(UserClaimTypes.AdminRole))
                {
                    <a asp-controller="Users" asp-action="Index">Brugere</a>
                }
                <a asp-controller="Account" asp-action="ChangePassword">@User.FindFirst(UserClaimTypes.DisplayName)?.Value</a>
                <form asp-controller="Account" asp-action="Logout" method="post">
                    <button type="submit">Log ud</button>
                </form>
            }
        </nav>
    </header>
    <main>
        @RenderBody()
    </main>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

```cshtml
@* views/Home/Error.cshtml *@
@model <projekt>.ViewModels.Home.ErrorHomeViewModel
@{
    ViewData["Title"] = "Fejl";
}
<h1>Der opstod en fejl</h1>
<p>Prøv igen om lidt. Kontakt support, hvis fejlen bliver ved, og oplys fejl-id'et.</p>
@if (!string.IsNullOrEmpty(Model.RequestId))
{
    <p>Fejl-id: <code>@Model.RequestId</code></p>
}
```

```cshtml
@* views/Home/Status.cshtml *@
@model <projekt>.ViewModels.Home.StatusHomeViewModel
@{
    ViewData["Title"] = Model.StatusCode == 404 ? "Siden findes ikke" : "Fejl";
}
<h1>@ViewData["Title"]</h1>
<p>Statuskode @Model.StatusCode.</p>
<p><a asp-controller="Home" asp-action="Index">Til forsiden</a></p>
```

```cshtml
@* views/<Navn>/Create.cshtml: mønster for en formular *@
@model <projekt>.ViewModels.<Navn>.Create<Navn>ViewModel
@{
    ViewData["Title"] = "Opret";
}
<h1>@ViewData["Title"]</h1>
<form asp-action="Create" method="post">
    <div asp-validation-summary="ModelOnly"></div>

    <label asp-for="Name"></label>
    <input asp-for="Name" />
    <span asp-validation-for="Name"></span>

    <button type="submit">Gem</button>
</form>
```

```css
/* wwwroot/css/site.css: minimum, projektet udbygger selv */
body { font-family: system-ui, sans-serif; margin: 0 auto; max-width: 960px; padding: 1rem; }
nav { display: flex; gap: 1rem; align-items: center; }
form label { display: block; margin-top: .75rem; }
.field-validation-error, .validation-summary-errors { color: #b00020; }
```

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
