# Swagger UI og OpenAPI

**Gælder for:** API-delen: alle API-projekter og alle kombinerede projekter. Et API har **altid** Swagger UI, uanset om det er et selvstændigt projekt eller en del af et web-projekt. Rene web-projekter har ingen Swagger.
**Kode:** `Program.cs` (se `program.md`: `Api:Name`, miljø-badge, `AddOpenApi`, redirect, `MapOpenApi`, `UseSwaggerUI`) og `infrastructure/OpenApi/ApiKeySecuritySchemeTransformer.cs`.
**Se også:** `apikeys.md` (Authorize-knappen), `configuration.md` (`Api:Name`), `program.md` (pakker og rækkefølge).

## Stier

| | API-projekt | Kombineret projekt |
|---|---|---|
| Swagger UI | `/swagger` | `/api/swagger` |
| Redirect til Swagger UI | `GET /` | `GET /api` (og `/api/`) |
| OpenAPI-dokument | `/openapi/v1.json` | `/api/openapi/v1.json` |
| Endpoints i dokumentet | Alle API-controllere | Kun API-controllerne under `/api`. Web-controllere bruger standard-routen og kommer derfor ikke med. |

I et kombineret projekt er `/` websidens forside, og alt, der hører til API'et, ligger under `/api`.

## Det skal hele API-delen have

| Funktion | Sådan |
|---|---|
| OpenAPI-dokument | `Microsoft.AspNetCore.OpenApi` serverer dokumentet, altid anonymt (`.AllowAnonymous()`), så Swagger UI kan hente det uden nøgle. |
| Swagger UI | `Swashbuckle.AspNetCore.SwaggerUI`, som peger på dokumentet. Brug ikke Swashbuckles egen dokument-generator (`AddSwaggerGen`). |
| Lukkede endpoints | `DocExpansion(DocExpansion.None)`: alle controller-sektioner er lukkede, når siden åbner. |
| Skjulte skemaer | `DefaultModelsExpandDepth(-1)`: "Schemas"-listen nederst vises ikke. Skemaerne kan stadig ses ved hvert endpoint. |
| Navn | `Api:Name` i `appsettings.json` læses én gang i `Program.cs` og bruges som `info.title`, som Swagger UI's side- og fanetitel (`DocumentTitle`) og som endpoint-label. Fallback er projektnavnet (`<projekt>`). Omdøbes API'et, ændres kun den indstilling. |
| Miljø-badge | `info.description` er et shields.io-badge ud fra `ASPNETCORE_ENVIRONMENT`: `brightgreen` for Production, `orange` for Development og `yellow` for alt andet. Det vises øverst i Swagger UI, så man kan se, hvilket miljø man er i. Det kræver, at browseren kan nå `img.shields.io`. |
| Ingen servers | `document.Servers` tømmes, så Swagger UI altid kalder den host, siden er hentet fra. |
| Authorize-knap | `ApiKeySecuritySchemeTransformer` tilføjer et `ApiKey`-security scheme (header `x-api-key`) og et globalt security requirement. Nøglen indsættes én gang og sendes med på alle "Try it out"-kald. |
| Redirect til Swagger UI | Almindelig middleware (`app.Use(...)`) lige før `MapOpenApi`, **ikke** et mapped endpoint. Den optræder derfor hverken i dokumentet, i Swagger UI eller i routing/authorization. Den rammer kun præcis `GET /` (API) eller `GET /api` (kombineret). |
| Alle miljøer | Dokumentet og Swagger UI er bevidst tilgængelige i alle miljøer, også Production. Kald til endpoints kræver stadig en nøgle. |

## Regler

1. API-delen får hele opsætningen fra `program.md` og transformeren nedenfor i samme ændring, som projektet (eller API-delen) oprettes. Ingen af delene må udelades.
2. Ret aldrig badge-farverne, `DocExpansion`, `DefaultModelsExpandDepth` eller `Api:Name`-logikken i ét projekt alene. Skal de ændres, så ret denne fil, `program.md` og alle projekter med en API-del i samme ændring. En bevidst afvigelse skal stå i `<projekt>/.claude/CLAUDE.md` med begrundelse.
3. `Api:Name` står altid i `appsettings.json` med API'ets visningsnavn.
4. Nye tilpasninger af dokumentet laves som document/operation transformers i `infrastructure/OpenApi/` og registreres i `AddOpenApi(...)`. Den eksisterende lambda med titel, badge og servers må blive i `Program.cs`.
5. OpenAPI-dokumentet skal forblive anonymt, ellers kan Swagger UI ikke starte.
6. Redirecten skal forblive middleware og må ikke laves om til et mapped endpoint.
7. Pakkeversioner: `Microsoft.OpenApi` skal ligge i det interval, `Microsoft.AspNetCore.OpenApi` kræver (til 10.0.x: 2.x). Den nyeste `Microsoft.OpenApi` (3.x) giver build-fejl (se `program.md`).

## Skabelon: `infrastructure/OpenApi/ApiKeySecuritySchemeTransformer.cs`

```csharp
using <projekt>.Infrastructure.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace <projekt>.Infrastructure.OpenApi;

public class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeId = "ApiKey";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiKeyAuthenticationOptions.HeaderName,
            Description = "API key required for every request. Use the master key from configuration, or a standard key issued via Keys/Create."
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeId, document)] = []
        });

        return Task.CompletedTask;
    }
}
```

Den bruger `ApiKeyAuthenticationOptions.HeaderName` fra `infrastructure/Authentication/` (se `apikeys.md`).

## Tjekliste: API-del i et nyt projekt

1. De tre pakker fra `program.md` i csproj, med `Microsoft.OpenApi` 2.x.
2. `Api:Name` i `appsettings.json`.
3. `apiName`, badge-variablerne og `AddOpenApi(...)` med begge transformers i `Program.cs`.
4. Redirect, `MapOpenApi(...).AllowAnonymous()` og `UseSwaggerUI(...)` fra `[Kun API]`- eller `[Kombineret]`-blokken i `program.md`.
5. `ApiKeySecuritySchemeTransformer` i `infrastructure/OpenApi/`.
6. Start projektet, og tjek, at redirecten sender til Swagger UI, at titlen er `Api:Name`, at badget viser `Development` i orange, at alle sektioner er lukkede, at "Schemas" ikke vises, at **Authorize** spørger efter `x-api-key`, og (kombineret) at dokumentet kun indeholder `/api/...`-stier.

## Kendte afvigelser i koden

Ingen. Dokument-transformeren med titel, badge og servers er en lambda direkte i `Program.cs`. Det er tilladt (regel 4).
