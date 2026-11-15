# Kommandoer

Kør fra løsningens rod, medmindre andet står. `<projekt>` er projektets mappenavn (fx `api`, `web`). Projektets egne URL'er og testeksempler står i `<projekt>/.claude/CLAUDE.md`.

## Build og kørsel

| Formål | Kommando |
|---|---|
| Restore (hele løsningen) | `dotnet restore` |
| Build (hele løsningen) | `dotnet build` |
| Kør et projekt (første launch-profil) | `dotnet run --project <projekt>` |
| Kør med en bestemt profil | `dotnet run --project <projekt> --launch-profile <profil>` (fx `https`) |
| Hot reload | `dotnet watch --project <projekt> run` |

URL'erne for hver profil står i `<projekt>/properties/launchSettings.json`.

## EF Core (kør fra projektmappen `<projekt>/`)

| Formål | Kommando |
|---|---|
| Ny migration | `dotnet ef migrations add <Navn> --output-dir data/Migrations --context <Projekt>DbContext` |
| Fjern seneste migration (kun hvis den ikke er deployet) | `dotnet ef migrations remove --context <Projekt>DbContext` |
| Opdatér databasen manuelt | `dotnet ef database update --context <Projekt>DbContext` |

- Migrations køres også automatisk ved opstart (se `dbcontext.md`).
- `dotnet ef` kræver det globale værktøj: `dotnet tool install --global dotnet-ef`. Brug ikke et lokalt tool-manifest, for `.config/` er gitignored.

## Manuel test

### API-delen
- **Swagger UI** på `/swagger` (kombineret: `/api/swagger`): klik **Authorize**, indsæt en `x-api-key` (master eller standard), og brug "Try it out".
- **curl:** `curl -H "x-api-key: <nøgle>" <url>/<Navn>/<Action>` (kombineret: `<url>/api/<Navn>/<Action>`).
- Master-nøglen til lokal test står i `ApiKeys:MasterKey` i projektets `appsettings.Development.json`. Det er en secret, så kopiér den aldrig ind i filer, der committes.

### Web-delen
- Åbn `/` i browseren, og log ind med `Users:InitialAdmin` fra `appsettings.Development.json`. Brugeren oprettes ved første opstart, når tabellen `Users` er tom.
- Med curl skal der bruges en cookie-jar, og alle POST'er skal have feltet `__RequestVerificationToken` fra formularen (anti-forgery). Ellers svarer appen 400.

## Tests

Om løsningen har testprojekter, står i `<projekt>/.claude/CLAUDE.md`. Et testprojekt oprettes sådan:

- Navngiv det `<projekt>.Tests` (fx `api.Tests`), giv det en reference til `<projekt>/<projekt>.csproj`, og tilføj det til løsningens `.slnx`.
- Kør alle tests: `dotnet test`. Kør én test: `dotnet test --filter FullyQualifiedName~<TestNavn>`.
