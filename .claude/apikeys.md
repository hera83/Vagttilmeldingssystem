# API-nøgler (`x-api-key`)

**Gælder for:** kun API-delen (API-projekter og `/api` i kombinerede projekter).
**Kode (standardkomponenter):** `infrastructure/Authentication/` (`ApiKeyAuthenticationOptions`, `ApiKeyAuthenticationHandler`, `ApiKeyClaimTypes`, `ApiKeyGenerator`), `repositories/ApiKeys/`, `data/Models/ApiKey.cs` + `data/Configurations/ApiKeyConfiguration.cs`, `controllers/KeysController.cs` + `dtos/Keys/`, `controllers/HealthController.cs` + `dtos/Health/`. I kombinerede projekter ligger controllerne i `controllers/Api/`.
**Se også:** `authentication.md` (overblik, policies, kombineret), `program.md` (registrering), `swagger.md` (Authorize-knappen), `errorhandling.md`.

## Sådan virker det

- Alle kald til API-delen kræver headeren `x-api-key` (`ApiKeyAuthenticationOptions.HeaderName`), medmindre endpointet har `[AllowAnonymous]`.
- `ApiKeyAuthenticationHandler` (scheme `"ApiKey"`) tjekker headeren i denne rækkefølge:
  1. **Master-nøglen** fra `ApiKeys:MasterKey`. Begge værdier hashes med SHA-256 og sammenlignes i konstant tid (`CryptographicOperations.FixedTimeEquals`), så hverken indhold eller længde lækker. Giver claimet `key_type=master`. Er værdien tom, er master-nøglen slået fra.
  2. **Tabellen `ApiKeys`** slås op på nøglens SHA-256-hash via `IApiKeyRepository.GetValidByHashAsync`. Rækken skal være `IsActive` og må ikke være udløbet (`ExpiresAt`, hvis den er sat). Giver claimene `key_type=standard` og `key_id=<id>`.
- Mangler headeren, svarer handleren `NoResult`, og fallback-policyen giver 401. En forkert nøgle giver også 401. Mangler rettigheden, svares der 403. Begge skrives som ProblemDetails via `IProblemDetailsService`, fordi basisklassen ellers sender en tom body.

## Nøgler

- `ApiKeyGenerator.GenerateKey()` laver rå nøgler: `ak_` + 64 hex-tegn (32 tilfældige bytes). `Hash()` giver SHA-256 (lowercase hex) til lagring, og `Preview()` giver de første 12 tegn + `…` til visning.
- Rå nøgler gemmes **aldrig** og vises kun én gang, i svaret fra `Create` og `Rollover` i `KeysController`. Kun hash og preview gemmes.
- Rollover giver en eksisterende række en ny nøgle (nyt hash og preview, `RolledOverAt` sættes). Den gamle nøgle holder op med at virke med det samme.

## Skabeloner: infrastructure

```csharp
// infrastructure/Authentication/ApiKeyAuthenticationOptions.cs
using Microsoft.AspNetCore.Authentication;

namespace <projekt>.Infrastructure.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "x-api-key";
}

// infrastructure/Authentication/ApiKeyClaimTypes.cs
public static class ApiKeyClaimTypes
{
    public const string KeyType = "key_type";
    public const string KeyId = "key_id";

    public const string Master = "master";
    public const string Standard = "standard";
}

// infrastructure/Authentication/ApiKeyGenerator.cs
using System.Security.Cryptography;
using System.Text;

public static class ApiKeyGenerator
{
    private const string Prefix = "ak_";

    public static string GenerateKey() => Prefix + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string key) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    public static string Preview(string key) => key[..Math.Min(12, key.Length)] + "…";
}

// infrastructure/Authentication/ApiKeyAuthenticationHandler.cs
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using <projekt>.Repositories.ApiKeys.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IConfiguration configuration,
    IApiKeyRepository apiKeyRepository,
    IProblemDetailsService problemDetailsService) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, loggerFactory, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var providedKey = Request.Headers[ApiKeyAuthenticationOptions.HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return AuthenticateResult.NoResult();
        }

        var providedHash = ApiKeyGenerator.Hash(providedKey);

        // 1. Master-nøglen. Begge sider hashes, så FixedTimeEquals altid sammenligner lige lange værdier.
        var masterKey = configuration["ApiKeys:MasterKey"];
        if (!string.IsNullOrWhiteSpace(masterKey) &&
            CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(providedHash), Encoding.ASCII.GetBytes(ApiKeyGenerator.Hash(masterKey))))
        {
            return Success(new Claim(ApiKeyClaimTypes.KeyType, ApiKeyClaimTypes.Master));
        }

        // 2. Standard-nøgler i tabellen ApiKeys.
        var apiKey = await apiKeyRepository.GetValidByHashAsync(providedHash, Context.RequestAborted);
        if (apiKey is null)
        {
            return AuthenticateResult.Fail("Ugyldig API-nøgle.");
        }

        return Success(
            new Claim(ApiKeyClaimTypes.KeyType, ApiKeyClaimTypes.Standard),
            new Claim(ApiKeyClaimTypes.KeyId, apiKey.Id.ToString()));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(StatusCodes.Status401Unauthorized, "API-nøgle mangler eller er ugyldig",
            $"Send en gyldig nøgle i headeren {ApiKeyAuthenticationOptions.HeaderName}.");

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        WriteProblemAsync(StatusCodes.Status403Forbidden, "Ingen adgang",
            "API-nøglen har ikke rettighed til dette endpoint.");

    private AuthenticateResult Success(params Claim[] claims)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private async Task WriteProblemAsync(int statusCode, string title, string detail)
    {
        Response.StatusCode = statusCode;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = Context,
            ProblemDetails = new ProblemDetails { Status = statusCode, Title = title, Detail = detail }
        });
    }
}
```

## Skabeloner: tabel

```csharp
// data/Models/ApiKey.cs
public class ApiKey : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPreview { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ContactName { get; set; }
    public string? ContactNote { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RolledOverAt { get; set; }
}

// data/Configurations/ApiKeyConfiguration.cs
public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.KeyHash).HasMaxLength(64);
        builder.Property(x => x.KeyPreview).HasMaxLength(20);
        builder.Property(x => x.ContactName).HasMaxLength(200);
        builder.Property(x => x.ContactNote).HasMaxLength(1000);
        builder.HasIndex(x => x.KeyHash).IsUnique();
    }
}
```

`<Projekt>DbContext` får `public DbSet<ApiKey> ApiKeys => Set<ApiKey>();`.

## Repository: `repositories/ApiKeys/`

Bygges efter `repositories.md`. Dtos:

| Dto | Felter |
|---|---|
| `ApiKeyDto` | `Id`, `Name`, `KeyPreview`, `IsActive`, `ContactName?`, `ContactNote?`, `ExpiresAt?`, `RolledOverAt?`, `CreatedAt`, `UpdatedAt`. Aldrig `KeyHash`. |
| `CreateApiKeyDto` | `Name`, `ContactName?`, `ContactNote?`, `ExpiresAt?` |
| `UpdateApiKeyDto` | `Name`, `IsActive`, `ContactName?`, `ContactNote?`, `ExpiresAt?` |
| `IssuedApiKeyDto` | `ApiKey` (`ApiKeyDto`) og `RawKey`. Returneres kun af `CreateAsync` og `RolloverAsync`. |

| Metode | Beskrivelse |
|---|---|
| `GetAllAsync()` | Alle nøgler sorteret efter navn. |
| `GetByIdAsync(Guid id)` | Én nøgle, eller `null`. |
| `GetValidByHashAsync(string keyHash)` | Aktiv, ikke-udløbet nøgle med det hash, eller `null`. Bruges kun af `ApiKeyAuthenticationHandler`. |
| `CreateAsync(CreateApiKeyDto dto)` | Genererer en rå nøgle, gemmer hash + preview og returnerer `IssuedApiKeyDto`. |
| `UpdateAsync(Guid id, UpdateApiKeyDto dto)` | Opdaterer, eller `null`. |
| `RolloverAsync(Guid id)` | Ny rå nøgle på eksisterende række, sætter `RolledOverAt`. `IssuedApiKeyDto` eller `null`. |
| `DeleteAsync(Guid id)` | `true`, hvis rækken fandtes. |

Mapperens `ToEntity` tager hash og preview som parametre: `dto.ToEntity(keyHash, keyPreview)`.

```csharp
// De metoder, der adskiller sig fra standard-skabelonen i repositories.md
public Task<ApiKeyDto?> GetValidByHashAsync(string keyHash, CancellationToken cancellationToken = default)
{
    var now = DateTime.UtcNow;
    return dbContext.ApiKeys.AsNoTracking()
        .Where(x => x.KeyHash == keyHash && x.IsActive && (x.ExpiresAt == null || x.ExpiresAt > now))
        .Select(ApiKeyMapper.ToDtoExpression)
        .FirstOrDefaultAsync(cancellationToken);
}

public async Task<IssuedApiKeyDto> CreateAsync(CreateApiKeyDto dto, CancellationToken cancellationToken = default)
{
    var rawKey = ApiKeyGenerator.GenerateKey();
    var entity = dto.ToEntity(ApiKeyGenerator.Hash(rawKey), ApiKeyGenerator.Preview(rawKey));
    dbContext.ApiKeys.Add(entity);
    await dbContext.SaveChangesAsync(cancellationToken);
    return new IssuedApiKeyDto { ApiKey = entity.ToDto(), RawKey = rawKey };
}

public async Task<IssuedApiKeyDto?> RolloverAsync(Guid id, CancellationToken cancellationToken = default)
{
    var entity = await dbContext.ApiKeys.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (entity is null)
    {
        return null;
    }

    var rawKey = ApiKeyGenerator.GenerateKey();
    entity.KeyHash = ApiKeyGenerator.Hash(rawKey);
    entity.KeyPreview = ApiKeyGenerator.Preview(rawKey);
    entity.RolledOverAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync(cancellationToken);
    return new IssuedApiKeyDto { ApiKey = entity.ToDto(), RawKey = rawKey };
}
```

## Controllere

`KeysController` har `[Authorize(Policy = AuthorizationPolicies.MasterKeyOnly)]` på klassen og bygges efter API-skabelonen i `controllers.md` med DTO'er i `dtos/Keys/`:

| Action | HTTP | Request | Response |
|---|---|---|---|
| `GetAll` | `GET` | – | `List<GetAllKeysResponseDto>` |
| `GetById` | `GET {id:guid}` | – | `GetByIdKeysResponseDto` eller 404 |
| `Create` | `POST` | `CreateKeysRequestDto` (`Name` påkrævet, højst 200, `ContactName?`, `ContactNote?`, `ExpiresAt?`) | `CreateKeysResponseDto` med den rå `Key` (201 via `CreatedAtAction`) |
| `Update` | `PUT {id:guid}` | `UpdateKeysRequestDto` | `UpdateKeysResponseDto` eller 404 |
| `Delete` | `DELETE {id:guid}` | – | 204 eller 404 |
| `Rollover` | `POST {id:guid}` | – | `RolloverKeysResponseDto` med den nye rå `Key`, eller 404 |

Kun `Create` og `Rollover` returnerer feltet `Key`. Alle andre svar viser kun `KeyPreview`.

```csharp
// controllers/HealthController.cs  (kombineret: controllers/Api/, route "api/[controller]/[action]" og [IgnoreAntiforgeryToken])
[ApiController]
[Route("[controller]/[action]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<GetHealthResponseDto> Get() =>
        Ok(HealthMapper.ToGetHealthResponseDto("Healthy", DateTime.UtcNow));
}

// dtos/Health/GetHealthResponseDto.cs
public class GetHealthResponseDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}

// dtos/Health/HealthMapper.cs
public static class HealthMapper
{
    public static GetHealthResponseDto ToGetHealthResponseDto(string status, DateTime timestampUtc) =>
        new() { Status = status, TimestampUtc = timestampUtc };
}
```

## Tjekliste: API-nøgler i et nyt projekt

1. Filerne i `infrastructure/Authentication/` ovenfor + `AuthorizationPolicies` (se `authentication.md`).
2. `ApiKey`-model og configuration, `DbSet` i `<Projekt>DbContext` og en migration (se `dbcontext.md`).
3. `repositories/ApiKeys/` med alle fire undermapper og `Documents/ApiKeys.md`.
4. `KeysController` + `dtos/Keys/` og `HealthController` + `dtos/Health/`.
5. `ApiKeys:MasterKey` i `appsettings.json` med tom værdi og en rigtig værdi i `appsettings.Development.json`.
6. `[Kun API]`- eller `[Kombineret]`-blokken for authentication og `MasterKeyOnly` i `Program.cs` (se `program.md`).

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
