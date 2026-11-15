# Løsningen: overblik og fælles konventioner

**Gælder for:** alle projekter i løsningen. Reglerne her gælder overalt, medmindre en område-fil eller projektets egen instruksfil siger noget mere specifikt.

## Løsningen

- ASP.NET Core MVC på `net10.0`, altid C#. Alt bygges med controllere. Brug aldrig minimal APIs eller Razor Pages.
- Én løsning (`.slnx` i roden) med ét eller flere projekter i hver sin undermappe, fx `api` eller `web`. Projekterne står i `.slnx`.
- Løsningens rod indeholder desuden `CLAUDE.md` + `.claude/` (instrukserne), `.project/` (planer i steps, se `planning.md`) og `.manuals/` (én manual pr. projekt, se `manuals.md`). Alle er gitignored (se `gitignore.md`).
- Standardstakken i alle projekter: SQLite + EF Core, Serilog og central fejlhåndtering. API-delen har desuden `x-api-key`-auth, OpenAPI og Swagger UI. Web-delen har views og cookie-login.
- Det er en **template**, og den indeholder kun instrukser, ingen kode. Når et projekt bestilles, bygges det ud fra reglerne og skabelonerne i område-filerne. Nye løsninger startes ved at kopiere mappen.

## Projekttyper

Hvert projekt har én af tre typer. Typen står øverst i `<projekt>/.claude/CLAUDE.md`.

| Type | Indhold | Login | Swagger UI | URL'er |
|---|---|---|---|---|
| **API** | Kun API-controllere | `x-api-key` | `/swagger`. `GET /` sendes dertil. | `/<Navn>/<Action>` |
| **Web** | MVC-controllere med views | Cookie | Ingen | `/<Navn>/<Action>` (standard-route) |
| **Kombineret** | Websider + et API under `/api` | Cookie på websider, `x-api-key` under `/api` | `/api/swagger`. `GET /api` sendes dertil. | Web: `/<Navn>/<Action>`, API: `/api/<Navn>/<Action>` |

Område-filerne bruger to begreber, der går på tværs af typerne:

- **API-delen** er et API-projekt eller `/api` i et kombineret projekt. Den har altid `x-api-key`, OpenAPI og Swagger UI, så API'et er nemt at finde rundt i.
- **Web-delen** er et web-projekt eller websiderne i et kombineret projekt. Den har altid views og cookie-login.

Skabelonerne markerer kode, der afhænger af typen:

| Markering | Kommer med i |
|---|---|
| `// [API-del]` | API-projekter og kombinerede projekter |
| `// [Web-del]` | Web-projekter og kombinerede projekter |
| `// [Kun API]` | Kun API-projekter |
| `// [Kun web]` | Kun web-projekter |
| `// [Kombineret]` | Kun kombinerede projekter |

Tag kun de blokke med, der passer til projektets type, og fjern selve markeringen i den færdige kode.

## Pladsholdere

De globale instrukser nævner aldrig konkrete features, services eller tabeller fra et bestemt projekt. De bruger pladsholdere i `<>`, og de erstattes altid med rigtige navne, når der skrives kode. En pladsholder må aldrig ende i kode eller i et filnavn.

| Pladsholder | Betyder | Eksempel på erstatning |
|---|---|---|
| `<projekt>` | Projektets mappenavn = csproj-navn = rod-namespace, præcis som det staves (små bogstaver) | `api`, `web` |
| `<Projekt>` | Samme navn i PascalCase, til klassenavne | `Api` → `ApiDbContext` |
| `<Entitet>` | En tabel/entitet i ental, PascalCase | `Customer` |
| `<Entiteter>` | Samme entitet i flertal = `DbSet`-navnet og repositoriets mappe | `Customers` |
| `<Navn>` | En controllers navn uden `Controller` = navnet på dens mappe i `dtos/`, `viewmodels/` og `views/`. I `bgworkers/` er det workerens navn uden `Worker`. | `Customers` |
| `<Action>` | En controller-actions metodenavn | `GetById`, `Edit` |
| `<Handling>` | Det, en service- eller manager-metode gør (bliver til `<Handling>Async`) | `Send` |
| `<Område>` | Et forretningsområde (managers og deres `Documents/`) | `Orders` |
| `<System>` | Et eksternt systems navn uden suffiks | `Payment` |
| `<Gruppe>` | En gruppe af endpoints i et eksternt API (og den matchende partial-fil) | `Invoices` |
| `<Feature>` | En features konfigurationssektion | `Payment` |
| `<Opgave>` | En undermappe i `app_files/` | `Attachments` |
| `<Del>` | En under-model inde i en DTO eller ViewModel | `OrderLine` |
| `<Emne>` | Et tværgående emne i `infrastructure/` eller en helper i `extensions/` | `Caching` |
| `<Type>` | Den type, en extension-klasse udvider | `String` |

En pladsholder med lille forbogstav er samme navn i camelCase til variabler og parametre: `<entitet>` → `customer`, `<system>` → `payment`. Ellers giver pladsholderne sig selv, hvor de bruges (fx `<Felt>`, `<sti>`, `<visningsnavn>`).

Stier i område-filerne er relative til projektmappen: `controllers/` betyder `<projekt>/controllers/`.

## Standardkomponenter

Template'en har nogle få komponenter med **faste navne**. De er en del af standarden og ikke eksempler. Område-filerne nævner dem derfor ved navn og har skabeloner eller en præcis beskrivelse af dem.

| Komponent | Findes i | Instruks |
|---|---|---|
| `<Projekt>DbContext`, `EntityBase`, `ISeeder`, `DataSeeder` | Alle projekter | `dbcontext.md` |
| `Program.cs` | Alle projekter | `program.md` |
| `GlobalExceptionHandler` | Alle projekter | `errorhandling.md` |
| `LogDatabaseSettings`, `repositories/Logs/` (`ILogRepository`), `LogCleanupWorker` | Alle projekter | `logging.md` |
| `AuthorizationPolicies` | Alle projekter | `authentication.md` |
| `ApiPath`, `CombinedAuthentication` | Kombinerede projekter | `authentication.md` |
| `ApiKeyAuthenticationHandler` m.fl., `repositories/ApiKeys/`, `ApiKey`-tabellen, `KeysController`, `HealthController` | API-delen | `apikeys.md` |
| `LogsController` | API-delen | `logging.md` |
| `ApiKeySecuritySchemeTransformer` | API-delen | `swagger.md` |
| `UserCookieEvents`, `UserPrincipalFactory`, `UserClaimTypes`, `repositories/Users/`, `User`-tabellen, `AccountController`, `UsersController` | Web-delen | `cookieauth.md` |
| `HomeController` (`Index`, `Error`, `Status`), `_ViewImports.cshtml`, `_ViewStart.cshtml`, `_Layout.cshtml`, `site.css` | Web-delen | `views.md` |

Alt andet (repositories, services, managers, workers, controllere, views) er projektspecifikt og står i `<projekt>/.claude/CLAUDE.md`. Et nyt projekt får de standardkomponenter, der passer til typen, i samme ændring, som projektet oprettes. De bygges ud fra skabelonerne med `<projekt>`-namespace.

## To lag af instrukser

| Lag | Fil | Indhold | Indlæses |
|---|---|---|---|
| Globalt | `.claude/*.md` i løsningens rod | Regler, der gælder alle projekter | Altid, via `CLAUDE.md` i roden |
| Projekt | `<projekt>/.claude/CLAUDE.md` | Projektets type, nøgletal, hvad der findes (databaser, repositories, services, workers, controllere, sider, konfiguration), forbilleder og projektets kendte afvigelser | Automatisk, første gang du læser eller retter en fil i projektet |

1. Skal du arbejde i et projekt, så læs `<projekt>/.claude/CLAUDE.md` først, hvis den ikke allerede er indlæst. Det gælder også, når du bare planlægger.
2. Ved konflikt vinder projektlaget over det globale lag. En bevidst afvigelse skal stå eksplicit i projektfilen med en begrundelse.
3. Projektspecifikke fakta skrives i projektfilen, aldrig i de globale filer. De globale filer bruger pladsholdere og nævner kun standardkomponenterne ved navn.
4. Når et nyt projekt tilføjes til løsningen, så vælg typen (spørg brugeren, hvis den ikke er givet), og opret `<projekt>/.claude/CLAUDE.md` efter skabelonen nederst. Filnavnet skal være `CLAUDE.md` med store bogstaver. Byg i samme ændring standardkomponenterne for typen: API-delen får `x-api-key` og Swagger UI (`apikeys.md`, `swagger.md`), og web-delen får views og cookie-login (`views.md`, `cookieauth.md`). Opret også projektets manual `.manuals/<projekt>/manual.md` (se `manuals.md`).
5. Hold projektfilen opdateret i samme ændring, når der tilføjes eller fjernes databaser, repositories, services, managers, workers, controllere, sider eller konfigurationssektioner.
6. Vil du følge en eksisterende implementering som forbillede (fx en full pattern-service), så find den i projektfilen. Står der ingen, så følg skabelonerne i område-filen, som er komplette nok til at stå alene.

## Mappekort (pr. projekt)

| Mappe | Indhold | Findes i | Instruks |
|---|---|---|---|
| `Program.cs` | Composition root: registreringer og pipeline | Alle | `program.md` |
| `.claude/CLAUDE.md` | Projektlaget | Alle | denne fil |
| `controllers/` | Controllere. I kombinerede projekter ligger API-controllerne i `controllers/Api/`. | Alle | `controllers.md` |
| `dtos/` | API-controllernes Request/Response-DTO'er + mappers | API-delen | `controllers.md` |
| `viewmodels/` | Web-controllernes ViewModels + mappers | Web-delen | `controllers.md` |
| `views/` | Razor-views (`.cshtml`) | Web-delen | `views.md` |
| `wwwroot/` | Statiske filer: css, js, billeder | Web-delen | `views.md` |
| `managers/` | Logik på tværs af repositories/services | Efter behov | `managers.md` |
| `repositories/` | Adgang til egen database og egne filer | Alle | `repositories.md` |
| `services/` | Eksterne systemer | Efter behov | `services.md` |
| `infrastructure/` | Tværgående kode: auth, fejl, logging, OpenAPI, routing | Alle | `infrastructure.md` |
| `extensions/` | Rene hjælpefunktioner | Efter behov | `extensions.md` |
| `bgworkers/` | Baggrundsjob | Alle | `bgworkers.md` |
| `data/` | EF Core: DbContext, Models, Configurations, Migrations, Seeds | Alle | `dbcontext.md` |
| `app_dbs/` | Databasefiler | Alle | `databases.md` |
| `app_files/` | Filer, appen gemmer | Alle | `files.md` |
| `properties/` | `launchSettings.json` | Alle | `configuration.md` |

Et projekt har kun de mapper, det har brug for. Opret en mappe, første gang den skal bruges.

## Hvem må kalde hvem

```text
controllers ─┐                ┌─► repositories ──► data/<Projekt>DbContext (app.db) + app_files/
             ├─► managers ────┤
bgworkers ───┘                └─► services ──────► eksterne systemer

controllers (web-del) ──► views: et view får kun sin ViewModel og kalder aldrig repositories, managers eller services.
Har en controller/bgworker kun én afhængighed, kalder den repository/service direkte (hybrid-reglen i managers.md).
extensions/ må bruges af alle. infrastructure/ kobles på i Program.cs.
```

## Navngivning

1. **Mappenavne i løsningens rod og i roden af hvert projekt skrives med små bogstaver.** Det gælder projektmapperne (`api`, `web` …) og topmapperne i et projekt: `app_dbs/`, `app_files/`, `bgworkers/`, `controllers/`, `data/`, `dtos/`, `extensions/`, `infrastructure/`, `managers/`, `properties/`, `repositories/`, `services/`, `viewmodels/`, `views/`, `wwwroot/`. Ingen undtagelser.
2. `app_dbs/` og `app_files/` staves præcis sådan med understregning og omdøbes aldrig.
3. Undermapper inde i topmapperne er PascalCase, medmindre et værktøj kræver andet: `dtos/<Navn>/`, `services/<System>/Dtos/`, `views/Shared/`, `controllers/Api/`.
4. Namespaces starter med `<projekt>` præcis som projektet staves, og resten er altid PascalCase, uanset hvordan mappen staves: `<projekt>.Controllers`, `<projekt>.Controllers.Api`, `<projekt>.Dtos.<Navn>`, `<projekt>.ViewModels.<Navn>`, `<projekt>.BgWorkers`, `<projekt>.Repositories.<Entiteter>.Mappers`. Eneste undtagelse er EF's genererede `<projekt>.data.Migrations` (se `dbcontext.md`).
5. Et namespace må aldrig hedde det samme som en klasse (det giver compile-fejlen CS0118). Derfor hedder mapper efter området uden suffiks (`<Entiteter>/`, `<System>/`), mens klasserne har suffikset (`<Entitet>Repository`, `<System>Service`).
6. Konsekvenser af små bogstaver, som skal håndteres:
   - `views/`: Razor leder som standard i `/Views/`, og Linux (Docker) skelner mellem store og små bogstaver. View-lokationerne sættes derfor til `/views/...` i `Program.cs` (se `views.md`).
   - `properties/`: .NET SDK'et leder efter `Properties/launchSettings.json`. Windows finder `properties/` alligevel, men på Linux og macOS finder `dotnet run` ikke launch-profilerne. Docker bruger dem ikke, så det påvirker kun lokal udvikling uden for Windows. `<AppDesignerFolder>properties</AppDesignerFolder>` i csproj løser det ikke, for `dotnet run` leder altid i `Properties/` (afprøvet med SDK 10.0.301). Brug den derfor ikke.

## Kodestil

1. File-scoped namespaces (`namespace <projekt>.Controllers;`).
2. Primary constructors til dependency injection.
3. `Nullable` er slået til: ikke-nullable strings får `= string.Empty`, og samlinger får `= []`.
4. Al I/O er async og tager `CancellationToken` som sidste parameter: `= default` i services, repositories og managers og uden default i controller-actions.
5. Tidspunkter er altid UTC (`DateTime.UtcNow`). Websider omregner kun til lokal tid ved visning.
6. Én klasse pr. fil, og filnavnet er klassenavnet. Partial-filer hedder `<Klasse>.<Gruppe>.cs`.
7. Tekster, brugeren ser (views, valideringsbeskeder, fejlbeskeder), skrives på projektets UI-sprog. Det står i `<projekt>/.claude/CLAUDE.md`. Står der intet, så er det dansk.

## `Program.cs`

Den fulde skabelon for alle tre typer, rækkefølgen og reglerne står i `program.md`.

## Skabelon: `<projekt>/.claude/CLAUDE.md`

````markdown
# Projekt: `<projekt>`

**Type:** API / Web / Kombineret (se `project.md`)
**Lag:** Projektlaget. De globale regler i løsningens `.claude/` gælder også.
**UI-sprog:** dansk (kun web-delen)

## Nøgletal
| | |
|---|---|
| Projektfil | `<projekt>/<projekt>.csproj` (rod-namespace `<projekt>`) |
| DbContext | `<Projekt>DbContext` |
| Lokale URL'er | fra `properties/launchSettings.json` |
| Swagger UI | `/swagger` (API) eller `/api/swagger` (kombineret) |
| Manual | `.manuals/<projekt>/manual.md` |
| Docker | projekt nr. X i sin serie, så port … |

## Databaser (`app_dbs/`)
| Fil | Indhold | Skrives af | Læses via |

## Repositories
| Mappe | Database/tabel | Bruges af |

## Managers
| Mappe | Formål | Bruger |

## Services
| Mappe | Mønster (full/light) | Bemærkning (fx forbillede, exception i `GlobalExceptionHandler`) |

## Workers
| Worker | Formål | Interval |

## Controllers og endpoints
| Controller | Del (API/web) | Adgang | Endpoints / sider |

## Infrastruktur (`infrastructure/`)
| Mappe | Indhold |

## Konfiguration
| Sektion | Bruges af | Secret-felter |

## Manuel test
- …

## Testprojekter
Ingen endnu.

## Bevidste afvigelser fra de globale regler
- … (med begrundelse)

## Kendte afvigelser i koden
- …
````

Udelad en sektion, hvis projektet ikke har noget af den slags, men skriv "Ingen endnu" frem for at slette den, når det er sandsynligt, at den kommer.
