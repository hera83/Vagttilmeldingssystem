# CLAUDE.md

Indgangen til løsningens AI-instrukser. Template'en indeholder kun instrukser og ingen kode. Projekterne bygges ud fra reglerne og skabelonerne, når de bestilles. Der er to lag:

- **Globalt lag:** `.claude/` i løsningens rod med én fil pr. område. Reglerne gælder alle projekter og alle tre projekttyper (API, Web og Kombineret, se `.claude/project.md`). Alle filerne importeres nederst i denne fil, så de altid er indlæst fra sessionens start.
- **Projektlag:** `<projekt>/.claude/CLAUDE.md` i hvert projekt med projektets type, hvad der findes i det, og projektets afvigelser. Den indlæses automatisk, første gang du læser eller retter en fil i projektet (se `.claude/project.md`).

## Sådan bruges instrukserne

1. Følg reglerne i den fil, der dækker det område, du arbejder i. Tabellen nedenfor viser, hvilken fil det er. Skal du arbejde i et projekt, så læs også `<projekt>/.claude/CLAUDE.md`, hvis den ikke allerede er indlæst.
2. **Ny kode følger reglerne, også når eksisterende kode gør noget andet.** Hver fil har afsnittet "Kendte afvigelser i koden". Ret dem kun, når brugeren beder om det.
3. Ved konflikt gælder: brugerens besked i chatten > projektlaget > det globale lag > eksisterende kode.
4. Er en regel uklar, eller dækker ingen fil situationen, så spørg brugeren i stedet for at gætte.
5. **Større ændringer planlægges først** som steps i `.project/` (se `.claude/planning.md`). Oversigten `.project/INDEX.md` er importeret nederst, så status, aktivt step og næste step altid er kendt fra sessionens start.
6. **Hver ændring i et projekt opdaterer projektets manual** i `.manuals/<projekt>/` i samme ændring (se `.claude/manuals.md`).
7. Hold instrukserne sande. Gør en ændring en instruks forkert, eller retter du en kendt afvigelse, så opdatér den relevante fil i samme ændring. Projektspecifikke fakta hører til i projektlaget, aldrig i de globale filer. Et nyt område kræver en ny fil i `.claude/`, en import-linje nederst og en række i tabellen. Et nyt projekt kræver en ny `<projekt>/.claude/CLAUDE.md`.

## Oversigt

| Fil | Dækker |
|---|---|
| `.claude/planning.md` | Planer i steps i `.project/`: hvornår, arbejdsgang, "næste step", statusværdier, skabeloner |
| `.claude/manuals.md` | Manualer i `.manuals/`: én pr. projekt, hvornår de opdateres, indhold, billeder, skabelon |
| `.claude/project.md` | Overblik, projekttyper (API/Web/Kombineret), pladsholdere, standardkomponenter, de to lag, mappekort, hvem må kalde hvem, navngivning, kodestil |
| `.claude/program.md` | `Program.cs` for alle tre typer, rækkefølge, pakker og versioner |
| `.claude/commands.md` | Build, kørsel, EF-kommandoer, manuel test, tests |
| `.claude/configuration.md` | appsettings, secrets, launchSettings, miljøer |
| `.claude/controllers.md` | API- og web-controllere, routes, API-DTO'er, ViewModels og deres mappers |
| `.claude/views.md` | Views, layout, `wwwroot/`, `HomeController` med fejl- og statusside (web-delen) |
| `.claude/managers.md` | Forretningslogik på tværs af repositories og services |
| `.claude/repositories.md` | Adgang til egen database |
| `.claude/services.md` | Eksterne systemer (full/light pattern) |
| `.claude/dbcontext.md` | EF Core: models, configurations, migrations, seeds, standardtabeller |
| `.claude/databases.md` | `app_dbs/` og databasefiler |
| `.claude/files.md` | `app_files/` og fil-metadata |
| `.claude/bgworkers.md` | Baggrundsjob |
| `.claude/extensions.md` | Generelle hjælpefunktioner |
| `.claude/infrastructure.md` | Tværgående kode |
| `.claude/authentication.md` | Overblik over login pr. type, fælles policies, kombinerede projekter, regler for nye endpoints og sider |
| `.claude/apikeys.md` | `x-api-key`: nøgler, handler, `KeysController`, `HealthController` (API-delen) |
| `.claude/cookieauth.md` | Cookie-login: brugere, `AccountController`, `UsersController` (web-delen) |
| `.claude/errorhandling.md` | ProblemDetails, fejlsider, statussider og exceptions |
| `.claude/logging.md` | Serilog, log.db, log-oprydning og log-søgning |
| `.claude/swagger.md` | Swagger UI og OpenAPI: lukkede endpoints, skjulte skemaer, miljø-badge (API-delen, obligatorisk) |
| `.claude/docker.md` | Docker-opsætning |
| `.claude/gitignore.md` | .gitignore |

## Instrukser

@.claude/planning.md
@.claude/manuals.md
@.claude/project.md
@.claude/program.md
@.claude/commands.md
@.claude/configuration.md
@.claude/controllers.md
@.claude/views.md
@.claude/managers.md
@.claude/repositories.md
@.claude/services.md
@.claude/dbcontext.md
@.claude/databases.md
@.claude/files.md
@.claude/bgworkers.md
@.claude/extensions.md
@.claude/infrastructure.md
@.claude/authentication.md
@.claude/apikeys.md
@.claude/cookieauth.md
@.claude/errorhandling.md
@.claude/logging.md
@.claude/swagger.md
@.claude/docker.md
@.claude/gitignore.md

## Planstatus

@.project/INDEX.md
