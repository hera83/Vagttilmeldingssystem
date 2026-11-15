# Step 01 - Init. løsning og web-projekt fra template'en

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | løsningen, web |
| Afhænger af | Ingen |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
En startbar løsning med projektet `web` (type **Web**), bygget efter template'en: cookie-login, brugeradministration, fejl- og statussider, Serilog, SQLite og et testprojekt. Alle senere steps bygger herpå. Krav: K01 (grundlag), K05 (roller).

## Omfang
- **Med:**
  - `Vagttilmeldingssystem.slnx` med `web/web.csproj` og `web.Tests/web.Tests.csproj`.
  - Alle standardkomponenter for et web-projekt (`project.md` → Standardkomponenter): `Program.cs` med `[Kun web]`-blokkene, `WebDbContext`, `EntityBase`, `ISeeder`, `DataSeeder`, `User`-tabellen, `GlobalExceptionHandler`, `LogDatabaseSettings`, `repositories/Logs/`, `repositories/Users/`, `LogCleanupWorker`, `AuthorizationPolicies` (kun `AdminOnly`), `UserClaimTypes`, `UserPrincipalFactory`, `UserCookieEvents`, `HomeController`, `AccountController`, `UsersController` med ViewModels og views, `_Layout`, `site.css`.
  - Første migration `InitialCreate`.
  - Grundlayout efter mockups og designtokens i `.project/krav/krav.md`: logo "Vagttilmelding", venstremenu til admin (indtil videre **Brugere**), topmenu til brugere og bundmenu på mobil. Ikoner som inline SVG (ingen CDN).
  - Login-siden designes efter mockup 01: kort forklaring af systemet og **Log ind**. Ingen "Kom i gang".
  - Brugernavnet er en e-mailadresse: `[EmailAddress]` og label "E-mail (brugernavn)" på oprettelsen i **Brugere**.
  - `appsettings.json` (alle nøgler, tomme værdier), `appsettings.Development.json` (lokale værdier, gitignored), `properties/launchSettings.json` (`http`/`https`).
  - `.env.example` i løsningens rod med alle secrets og miljøspecifikke nøgler i `__`-notation og tomme værdier (nu `Users__InitialAdmin__UserName` og `Users__InitialAdmin__Password`). Den holdes opdateret i hvert step, der tilføjer konfiguration (`configuration.md` regel 8), så projektet hele tiden er klar til Docker.
  - `app_dbs/.gitkeep`, `app_files/.gitkeep` og `.gitignore` efter `gitignore.md` (inkl. `.env`, `.env.*` og `!.env.example`).
  - `web.Tests` (xUnit + `Microsoft.AspNetCore.Mvc.Testing`) med smoke-tests og en fælles test-fabrik, der kører appen mod en midlertidig SQLite-fil.
  - Projektlaget `web/.claude/CLAUDE.md` og manualen `.manuals/web/manual.md`.
- **Ikke med:** Profiler (step 02), begivenheder og vagter (step 03+), PWA (step 09), Dockerfile og compose (step 16).

## Plan
1. [ ] Opret `.slnx`, `web/web.csproj` (net10.0, pakkerne fra `program.md` med de afprøvede versioner) og `web.Tests/web.Tests.csproj`.
2. [ ] Byg `data/`: `WebDbContext`, `EntityBase`, `User` + `UserConfiguration`, `ISeeder`, `DataSeeder` (tom `Seeders`-liste).
3. [ ] Byg `infrastructure/` (Authentication, ErrorHandling, Logging) efter skabelonerne.
4. [ ] Byg `repositories/Users/` og `repositories/Logs/` med `Documents/`, og `bgworkers/LogCleanupWorker.cs`.
5. [ ] Byg `HomeController`, `AccountController` og `UsersController` med ViewModels og views. Brugernavn med e-mailvalidering.
6. [ ] `Program.cs` efter skabelonen (`[Kun web]`), inkl. første administrator fra `Users:InitialAdmin`.
7. [ ] `_Layout.cshtml` og `site.css` med designtokens: admin-venstremenu, brugermenu, bundmenu på mobil, statusbadges, kort, tabeller, formularer, knapper. Login-side efter mockup 01.
8. [ ] Konfiguration: `appsettings.json`, `appsettings.Development.json`, `launchSettings.json` og `.env.example`.
9. [ ] Migration `InitialCreate` (`dotnet ef migrations add InitialCreate --output-dir data/Migrations --context WebDbContext`).
10. [ ] `.gitignore`, `app_dbs/.gitkeep`, `app_files/.gitkeep`.
11. [ ] `web.Tests`: test-fabrik (midlertidig content root og database) og smoke-tests: `/` sender til login, login med initial admin virker, en bruger uden admin får "Ingen adgang" på `/Users`, en ukendt side viser statussiden 404.
12. [ ] Opret `web/.claude/CLAUDE.md` efter skabelonen i `project.md`: type Web, UI-sprog dansk, nøgletal, tabellerne og afsnittet "Forretningsregler" med henvisning til `.project/krav/krav.md`. Under "Bevidste afvigelser": brugernavn = e-mail.
13. [ ] Opret manualen `.manuals/web/manual.md` efter skabelonen (Overblik, Kom i gang, Login, Brugere, Konfiguration og drift), skærmbillede af login-siden og rækken i `.manuals/README.md`.
14. [ ] Opdatér Status for K01/K05 i `.project/krav/krav.md`.
15. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 01 - Init. løsning og web-projekt fra template'en" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `Vagttilmeldingssystem.slnx`, `.gitignore`, `.env.example` (nye)
- `web/` (nyt projekt med alle standardkomponenter)
- `web.Tests/` (nyt testprojekt)
- `web/.claude/CLAUDE.md`, `.manuals/web/manual.md`, `.manuals/README.md` (nye/ændret)

## Beslutninger
- Projekttype Web, projektnavn `web`, DbContext `WebDbContext`, visningsnavn "Vagttilmelding" (truffet af brugeren: Web).
- Brugernavnet er forælderens e-mailadresse, fordi e-mails sendes til brugernavnet (step 11) (foreslået af Claude).
- Ingen offentlig forside. Login-siden har forklaringen og PWA-info fra mockup 01, fordi kun admin opretter brugere, og template'en kræver login overalt (foreslået af Claude).
- Testprojektet oprettes allerede nu, fordi først-til-mølle og skjulte vagter kræver integrationstest i næsten alle steps (foreslået af Claude).
- `.env.example` oprettes allerede nu og følger med i hvert step, fordi projektet skal forberedes til Docker (truffet af brugeren).
- Hvert step afsluttes med commit og push (truffet af brugeren).

## Test
- `dotnet build` og `dotnet test` fra løsningens rod.
- `dotnet run --project web`: `/` sender videre til login, login med `Users:InitialAdmin` virker, **Brugere** kan oprette en bruger, en bruger uden admin får "Ingen adgang" på `/Users`, `/findes-ikke` viser statussiden.
- Skærmbillede af login-siden med `msedge --headless --screenshot`.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Git: brugeren initialiserer repository og remote. Er det ikke gjort, når steppet skal committes, så spørg brugeren.
