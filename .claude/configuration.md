# Konfiguration og secrets

**Gælder for:** alle projekter: `appsettings*.json`, `properties/launchSettings.json`, miljøer og secrets.
**Se også:** `docker.md` (.env), `gitignore.md`, `services.md` (hvordan services læser konfiguration).

## Filer

| Fil | Indhold | I git? |
|---|---|---|
| `appsettings.json` | **Alle** nøgler med tomme eller ufarlige standardværdier. Oversigten over, hvad der kan konfigureres. | Ja |
| `appsettings.Development.json` | Rigtige værdier til lokal udvikling, inkl. secrets i klartekst. | **Nej** (gitignored) |
| `properties/launchSettings.json` | Launch-profilerne `http` og `https`. Sætter `ASPNETCORE_ENVIRONMENT=Development`. | Ja |
| `.env` (kun Docker) | De rigtige værdier i containeren. | **Nej** (se `docker.md`) |

## Regler

1. En ny indstilling tilføjes **altid** i `appsettings.json` med en tom eller ufarlig standardværdi, så alle nøgler kan ses ét sted. Rigtige værdier står kun i `appsettings.Development.json` (lokalt) og i `.env`/miljøvariabler (drift).
2. Secrets (API-nøgler, passwords, tokens) må aldrig stå i `appsettings.json` eller i andre filer, der committes.
3. `appsettings.Development.json` indeholder rigtige credentials (hvilke står i `<projekt>/.claude/CLAUDE.md`). Behandl filen som en secret, og citér, kopiér eller log aldrig dens værdier.
4. Vil brugeren flytte secrets ud af `appsettings.Development.json`, så brug `dotnet user-secrets` eller miljøvariabler.
5. Én sektion pr. service/feature, opkaldt efter den: `<System>` for en service og `<Feature>` for andet. Underopdel med `:` (`<System>:<Del>`), når en service har flere forbindelser. Miljøvariabler og `.env` bruger `__` som separator (`<System>__ApiKey`).
6. Full pattern-services læser deres sektion via deres `<System>ConfigurationProvider`. Light pattern-services læser `IConfiguration` direkte (se `services.md`).
7. Kode, der kun må køre i Development, tjekker `IHostEnvironment.IsDevelopment()`.
8. Tilføjes, ændres eller fjernes en sektion, så opdatér tabellen Konfiguration i `<projekt>/.claude/CLAUDE.md` og `.env.example`, hvis den findes.

## Standardsektioner

| Sektion | Bruges af |
|---|---|
| `ConnectionStrings:Default` | `<Projekt>DbContext` (`app.db`). `Data Source` er relativ og gøres absolut ud fra `ContentRootPath` i `Program.cs`. |
| `Api:Name` | API-delen: OpenAPI-titel og Swagger UI (se `swagger.md`). |
| `ApiKeys:MasterKey` | API-delen: master-nøglen (se `apikeys.md`). Secret. |
| `Users:InitialAdmin:UserName` og `Users:InitialAdmin:Password` | Web-delen: første administrator, når tabellen `Users` er tom (se `cookieauth.md`). `Password` er en secret. Kan fjernes, når den første administrator er oprettet. |
| `Logging:LogLevel` | Har ingen effekt på Serilog. Niveauerne sættes i `Program.cs` (se `logging.md`). |

Alle sektioner i et projekt (inkl. services og deres secrets) står i `<projekt>/.claude/CLAUDE.md`.

## `properties/launchSettings.json`

- Har profilerne `http` og `https`, begge med `ASPNETCORE_ENVIRONMENT=Development`.
- Mappen staves `properties/` (små bogstaver, se `project.md`). SDK'et leder efter `Properties/launchSettings.json`. Windows finder filen alligevel, men på Linux og macOS kører `dotnet run` uden profilerne. Brug så `--urls` og `ASPNETCORE_ENVIRONMENT=Development` på kommandolinjen.

## Miljøer

- `ASPNETCORE_ENVIRONMENT` er `Development` i launch-profilerne og `Production` i Docker.
- I API-delen er Swagger UI og OpenAPI tilgængelige i alle miljøer (se `swagger.md`).
- I web-delen er login-cookien altid `Secure` i Production, så websider i drift skal serveres over https, fx via en reverse proxy (se `docker.md`).

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
