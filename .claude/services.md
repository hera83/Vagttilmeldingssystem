# Services (`services/`)

**Gælder for:** `services/` i alle projekter, integrationer med eksterne systemer (HTTP-API'er, FTP, mail, SMS …).
**Se også:** `repositories.md` (egen database), `managers.md` (logik, der kombinerer services og repositories), `errorhandling.md` (exceptions med statuskode), `configuration.md` (settings og secrets), `infrastructure.md` (tværgående kode).

## Formål

En service er den eneste kode, der taler med ét bestemt eksternt system. Den skjuler protokol, URL'er, auth og fejlformat bag et interface med almindelige C#-metoder.

`<System>` er det eksterne systems navn i PascalCase uden suffiks. Projektets eksisterende services, og hvilke af dem der er gode forbilleder, står i `<projekt>/.claude/CLAUDE.md`.

## Struktur

```text
<projekt>/services/
└── <System>/                                   ← namespace <projekt>.Services.<System>
    ├── <System>Service.cs                      ← implementering
    ├── <System>Service.<Gruppe>.cs             ← kun full pattern med mange endpoints: én partial-fil pr. <Gruppe>
    ├── <System>Settings.cs                     ← kun full pattern
    ├── <System>ConfigurationProvider.cs        ← kun full pattern
    ├── <System>HttpClientFactory.cs            ← kun full pattern
    ├── <System>Exception.cs                    ← alle mønstre (regel 14)
    ├── Documents/
    │   ├── <System>.md                         ← AI-beskrivelse (skabelon nederst)
    │   └── swagger.json                        ← det eksterne API's spec, hvis den findes
    ├── Dtos/                                   ← namespace <projekt>.Services.<System>.Dtos.<Gruppe>
    │   ├── <Gruppe>/                           ← én undermappe pr. endpoint-gruppe i det eksterne API
    │   │   ├── <Handling>RequestDto.cs
    │   │   └── <Handling>ResponseDto.cs
    │   ├── Common/                             ← typer, som flere grupper deler
    │   └── Errors/
    │       └── ErrorResponseDto.cs             ← det eksterne API's fejl-body (når det har en)
    └── Interfaces/                             ← namespace <projekt>.Services.<System>.Interfaces
        ├── I<System>Service.cs
        └── I<System>ConfigurationProvider.cs   ← kun full pattern
```

## Regler

### Ansvar
1. `services/` er **kun** til eksterne systemer. Tværgående kode (auth, fejlhåndtering, logging-opsætning) hører til i `infrastructure/`.
2. En service rører aldrig projektets egen database: ingen `<Projekt>DbContext` og ingen repositories. Skal noget fra et eksternt system gemmes, så gør kalderen det via et repository (se `managers.md`).
3. Før du opretter en ny service eller metode, så læs `services/*/Documents/*.md` og tjek, om noget eksisterende allerede dækker behovet.

### Navngivning
4. Mappen hedder `<System>/` uden suffiks, klassen `<System>Service` og interfacet `I<System>Service`. Et namespace må aldrig hedde det samme som en klasse.
5. Namespaces: `<projekt>.Services.<System>` med `.Dtos.<Gruppe>` og `.Interfaces`.
6. Undermapperne `Documents/`, `Dtos/` og `Interfaces/` oprettes altid. Én klasse pr. fil.

### Metoder
7. Én metode pr. eksternt endpoint. Alle er `async`, ender på `Async` og tager `CancellationToken cancellationToken = default` som sidste parameter.
8. Streaming-endpoints returnerer `IAsyncEnumerable<T>` med `[EnumeratorCancellation]` på tokenet.
9. Metoder tager og returnerer servicens egne Dtos. Aldrig controllerens API-DTO'er og aldrig EF-entities.

### Dtos
10. Én klasse pr. fil med suffikset `Dto`, `RequestDto` eller `ResponseDto`, grupperet i `Dtos/<Gruppe>/`, der matcher det eksterne API's endpoint-grupper. Navnene har ikke systemets navn som præfiks (`<Handling>RequestDto`, ikke `<System><Handling>RequestDto`). Typer, som flere grupper deler, ligger i `Dtos/Common/`, og API'ets fejl-body i `Dtos/Errors/`.
11. Brug `class`/`sealed class`. Ikke-nullable strings får `= string.Empty` og samlinger `= []`. Kun reelt valgfrie felter er nullable.
12. Brug `[JsonPropertyName("...")]`, når wire-formatet ikke matcher serializerens naming policy. Validerings-attributter er tilladt på request-DTO'er.
13. Har to grupper en Dto med samme navn, så brug et `using`-alias i interfacet og servicen (`using <Gruppe>ListResponseDto = <projekt>.Services.<System>.Dtos.<Gruppe>.ListResponseDto;`) i stedet for at omdøbe Dto'en.

### Fejl
14. **Alle** services, også light pattern, har deres egen `<System>Exception` med en `HttpStatusCode`, som appen skal svare med. Fejl fra det eksterne system kastes altid som den, aldrig som `HttpRequestException`, `JsonException` eller bibliotekets egne exceptions. Fang den aldrig lokalt. Tilføj én `case` i `GlobalExceptionHandler` (se `errorhandling.md`). Ellers bliver fejlen til en anonym 500.
15. Oversæt fejl, så klienten ikke bliver vildledt:
    - **Manglende konfiguration** (fx tom `<System>:BaseUrl` eller `<System>:Host`) → `500 InternalServerError` med konfigurationsnøglens navn i beskeden. Tjek det, før forbindelsen åbnes, så det ikke bliver en `UriFormatException` eller lignende.
    - **HTTP-systemer:**
      - Systemet kan ikke nås (`HttpRequestException`) → `502 BadGateway`.
      - Timeout (`TaskCanceledException`, uden at kalderens token er annulleret) → `504 GatewayTimeout`. Nævn konfigurationsnøglen for timeout i beskeden, hvis der er en.
      - Svaret kan ikke deserialiseres til Dto'en → `502 BadGateway`, med endpoint og typenavn i beskeden. Det gælder også hver linje i et streaming-svar.
      - `401` fra systemet handler om **servicens egen** nøgle, ikke om klientens. Send den aldrig uændret videre: svar `502 BadGateway` og nævn konfigurationsnøglen (`<System>:ApiKey`).
      - `403` fra systemet videresendes som `403` med en besked om, at servicens nøgle mangler rettigheder i det eksterne system.
      - Alt andet videresendes med systemets statuskode og fejlbesked (fra `ErrorResponseDto`, ellers den rå body).
    - **Systemer uden HTTP** (SMTP/IMAP, FTP, SDK'er):
      - Forbindelsen kan ikke oprettes, eller TLS-forhandlingen fejler → `502 BadGateway` med host, port og de konfigurationsnøgler, der styrer forbindelsen.
      - Login afvises → `502 BadGateway` med navnene på konfigurationsnøglerne for brugernavn og adgangskode, aldrig værdierne.
      - Timeout → `504 GatewayTimeout`.
      - Protokol- eller serverfejl → `502 BadGateway` med serverens svar.
      - Fejl i kalderens input, som systemet afviser (fx en ugyldig modtager), → `400 BadRequest`. Noget, der ikke findes (fx en mappe), → `404 NotFound`.
    - Ukendte exceptions (programmeringsfejl) oversættes ikke. De skal blive til en 500 i `GlobalExceptionHandler`.
16. Exception-beskeder må aldrig indeholde secrets (nøgler, passwords, tokens).

### Documents
17. `Documents/<System>.md` oprettes sammen med servicen og opdateres **i samme ændring**, når public metoder ændres.
18. Findes der en spec for det eksterne API (OpenAPI/swagger), så gem den i `Documents/`.

## Full eller light pattern

**Full pattern** er standard for eksterne HTTP-API'er. Brug det især ved mange endpoints, eller når base-URL, API-nøgle eller timeout kan skulle ændres i runtime.

| Fil | Indhold |
|---|---|
| `<System>Settings.cs` | POCO med fornuftige defaults for alle værdier. |
| `<System>ConfigurationProvider.cs` + interface | `GetActiveConfigurationAsync(ct)` returnerer `<System>Settings` fra `IConfiguration`. Kilden kan senere skiftes (fx DB eller pr. tenant) uden at røre servicen. |
| `<System>HttpClientFactory.cs` | Wrapper om `IHttpClientFactory.CreateClient("<System>")`, der sætter `BaseAddress`, timeout og auth-header ved hvert kald. |
| `<System>Exception.cs` | Bærer `HttpStatusCode` og evt. upstream-statuskoden. Udvid med flere felter fra det eksterne systems fejlsvar efter behov. |
| `<System>Service.cs` | Statisk `JsonSerializerOptions` med det eksterne API's naming policy og private helpers (`GetAsync<T>`, `PostAsync<TReq,TRes>`, `SendAsync`, `EnsureSuccessAsync` …). Ved mange endpoints: `partial class`, hvor de public metoder ligger i `<System>Service.<Gruppe>.cs`, der matcher `Dtos/<Gruppe>/`. |

**Light pattern** bruges kun, når forbindelsesdetaljerne aldrig skal ændres i runtime. Det består kun af `Interfaces/I<System>Service.cs`, `<System>Service.cs` og `<System>Exception.cs` (+ `Documents/` og `Dtos/`). Der er to varianter:

- **Light med HTTP:** typed `HttpClient`, hvor `BaseAddress` sættes i `Program.cs`, og API-nøglen læses fra `IConfiguration` i constructoren. Servicen har de samme private helpers som full pattern (`SendAsync`, `EnsureSuccessAsync`, `ReadJsonAsync`), så fejlene oversættes efter regel 15.
- **Light uden HTTP:** fx SMTP/IMAP, FTP eller et SDK. Ingen `HttpClient`. `IConfiguration` læses ved hvert kald. Hver public metode kører gennem én privat helper, der oversætter bibliotekets exceptions efter regel 15.

## Skabelon: full pattern

```csharp
// <System>Settings.cs
namespace <projekt>.Services.<System>;

public class <System>Settings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 100;
}

// Interfaces/I<System>ConfigurationProvider.cs
namespace <projekt>.Services.<System>.Interfaces;

public interface I<System>ConfigurationProvider
{
    Task<<System>Settings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default);
}

// <System>ConfigurationProvider.cs
public class <System>ConfigurationProvider(IConfiguration configuration) : I<System>ConfigurationProvider
{
    public Task<<System>Settings> GetActiveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var settings = new <System>Settings
        {
            BaseUrl = configuration["<System>:BaseUrl"] ?? string.Empty,
            ApiKey = configuration["<System>:ApiKey"],
            RequestTimeoutSeconds = int.TryParse(configuration["<System>:RequestTimeoutSeconds"], out var timeout) && timeout > 0 ? timeout : 100
        };

        return Task.FromResult(settings);
    }
}

// <System>HttpClientFactory.cs
public class <System>HttpClientFactory(IHttpClientFactory httpClientFactory)
{
    public HttpClient Create(<System>Settings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            throw new <System>Exception(HttpStatusCode.InternalServerError, "<System>:BaseUrl er ikke sat.");
        }

        var client = httpClientFactory.CreateClient("<System>");
        client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        client.Timeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds);

        // Header-navnet og formatet afhænger af det eksterne API (fx "X-Api-Key" eller "Authorization: Bearer …").
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
        }

        return client;
    }
}

// <System>Exception.cs
public class <System>Exception(
    HttpStatusCode statusCode,
    string message,
    HttpStatusCode? upstreamStatusCode = null) : Exception(message)
{
    /// <summary>Statuskoden, appen svarer med (se GlobalExceptionHandler).</summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>Statuskoden fra det eksterne system, eller null hvis det ikke kunne nås.</summary>
    public HttpStatusCode? UpstreamStatusCode { get; } = upstreamStatusCode;
}

// <System>Service.cs
public partial class <System>Service(
    I<System>ConfigurationProvider configurationProvider,
    <System>HttpClientFactory httpClientFactory) : I<System>Service
{
    // Naming policy efter det eksterne API's wire-format (fx CamelCase eller SnakeCaseLower).
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private async Task<HttpClient> CreateClientAsync(CancellationToken cancellationToken)
    {
        var settings = await configurationProvider.GetActiveConfigurationAsync(cancellationToken);
        return httpClientFactory.Create(settings);
    }

    private static async Task<TResponse> GetAsync<TResponse>(HttpClient client, string endpoint, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(client, new HttpRequestMessage(HttpMethod.Get, endpoint), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<TResponse>(response, cancellationToken);
    }

    private static async Task<TResponse> PostAsync<TRequest, TResponse>(HttpClient client, string endpoint, TRequest request, CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(request, JsonOptions), Encoding.UTF8, "application/json")
        };
        using var response = await SendAsync(client, message, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadJsonAsync<TResponse>(response, cancellationToken);
    }

    // Alle kald går herigennem, så netværksfejl og timeouts bliver til 502/504 (regel 15).
    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage message, CancellationToken cancellationToken)
    {
        try
        {
            return await client.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new <System>Exception(HttpStatusCode.BadGateway, $"Kunne ikke forbinde til <System> ({client.BaseAddress}): {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new <System>Exception(HttpStatusCode.GatewayTimeout,
                $"<System> svarede ikke inden for {client.Timeout.TotalSeconds:0} sekunder. Timeout styres af <System>:RequestTimeoutSeconds.");
        }
    }

    private static async Task<TResponse> ReadJsonAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            return JsonSerializer.Deserialize<TResponse>(payload, JsonOptions) ?? throw new JsonException("Svaret var tomt.");
        }
        catch (JsonException ex)
        {
            throw new <System>Exception(HttpStatusCode.BadGateway,
                $"Uventet svar fra <System> på {response.RequestMessage?.RequestUri?.AbsolutePath}: kunne ikke læses som {typeof(TResponse).Name}. {ex.Message}");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = string.IsNullOrWhiteSpace(raw) ? $"<System> svarede {(int)response.StatusCode}." : raw;
        try
        {
            var error = JsonSerializer.Deserialize<ErrorResponseDto>(raw, JsonOptions);
            message = error?.Detail ?? error?.Title ?? message;
        }
        catch (JsonException)
        {
            // Body'en er ikke API'ets fejlformat. Brug den rå tekst.
        }

        var endpoint = response.RequestMessage?.RequestUri?.AbsolutePath;
        throw response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new <System>Exception(HttpStatusCode.BadGateway,
                $"<System> afviste servicens nøgle på {endpoint} (<System>:ApiKey mangler, er ugyldig eller udløbet). Svar: {message}", response.StatusCode),
            HttpStatusCode.Forbidden => new <System>Exception(HttpStatusCode.Forbidden,
                $"Nøglen i <System>:ApiKey har ikke adgang til {endpoint} i <System>. Svar: {message}", response.StatusCode),
            _ => new <System>Exception(response.StatusCode, message, response.StatusCode)
        };
    }
}

// <System>Service.<Gruppe>.cs: de public metoder for én endpoint-gruppe
public partial class <System>Service
{
    public async Task<<Handling>ResponseDto> <Handling>Async(<Handling>RequestDto request, CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(cancellationToken);
        return await PostAsync<<Handling>RequestDto, <Handling>ResponseDto>(client, "<sti>", request, cancellationToken);
    }
}
```

`<Handling>` er endpointets handling (fx `Send`, `GetStatus`), og `<sti>` er endpointets relative sti i det eksterne API. Tilføj flere helpers (`PutAsync`, `DeleteAsync`, multipart, fil-download med `HttpCompletionOption.ResponseHeadersRead`) efter behov, og lad dem alle gå gennem `SendAsync` og `EnsureSuccessAsync`.

## Skabelon: light pattern med HTTP

```csharp
public class <System>Service(HttpClient httpClient, IConfiguration configuration) : I<System>Service
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string? _apiKey = configuration["<System>:ApiKey"];

    // Public metoder kalder httpClient med relative stier. BaseAddress er sat i Program.cs.
    // Alle kald går gennem SendAsync, EnsureSuccessAsync og ReadJsonAsync som i full pattern-skabelonen,
    // men SendAsync tjekker først, at BaseAddress er sat:
    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        if (httpClient.BaseAddress is null)
        {
            throw new <System>Exception(HttpStatusCode.InternalServerError, "<System>:BaseUrl er ikke sat.");
        }

        try
        {
            return await httpClient.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new <System>Exception(HttpStatusCode.BadGateway, $"Kunne ikke forbinde til <System> ({httpClient.BaseAddress}): {ex.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new <System>Exception(HttpStatusCode.GatewayTimeout, $"<System> svarede ikke inden for {httpClient.Timeout.TotalSeconds:0} sekunder.");
        }
    }
}
```

## Skabelon: light pattern uden HTTP

```csharp
public class <System>Service(IConfiguration configuration) : I<System>Service
{
    public Task<<Handling>ResponseDto> <Handling>Async(<Handling>RequestDto request, CancellationToken cancellationToken = default) =>
        RunAsync(async () =>
        {
            var host = configuration["<System>:Host"];
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new <System>Exception(HttpStatusCode.InternalServerError, "<System>:Host er ikke sat.");
            }

            // … forbind, log ind og udfør handlingen med bibliotekets klient …
        }, cancellationToken);

    // Alle public metoder går herigennem, så bibliotekets exceptions oversættes ét sted (regel 15).
    private static async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action();
        }
        catch (<System>Exception)
        {
            throw;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TimeoutException ex)
        {
            throw new <System>Exception(HttpStatusCode.GatewayTimeout, $"<System> svarede ikke i tide: {ex.Message}");
        }
        catch (SocketException ex)
        {
            throw new <System>Exception(HttpStatusCode.BadGateway, $"Kunne ikke forbinde til <System> (<System>:Host og <System>:Port): {ex.Message}");
        }
        // … én catch pr. kendt exception fra biblioteket (login, protokol, ikke fundet). Ukendte exceptions fanges ikke.
    }
}
```

## Registrering i `Program.cs`

Alle registreringer står under `// Eksterne services`.

```csharp
// Full pattern (builder.Services.AddHttpClient() findes allerede og skal kun stå der én gang)
builder.Services.AddScoped<I<System>ConfigurationProvider, <System>ConfigurationProvider>();
builder.Services.AddScoped<<System>HttpClientFactory>();
builder.Services.AddScoped<I<System>Service, <System>Service>();

// Light med HTTP
builder.Services.AddHttpClient<I<System>Service, <System>Service>((sp, client) =>
{
    var baseUrl = sp.GetRequiredService<IConfiguration>()["<System>:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }
});

// Light uden HTTP
builder.Services.AddScoped<I<System>Service, <System>Service>();
```

Konfigurationen ligger i sektionen `<System>` i `appsettings.json` med tomme værdier (se `configuration.md`).

## Skabelon: `Documents/<System>.md`

````markdown
# <System> (service)

**Formål:** …
**Brug det når:** opgaven skal hente fra eller sende til <System>.
**Brug det ikke til:** data i appens egen database (det er et repository).
**Eksternt system og konfiguration:** <kort beskrivelse af systemet>, sektion `<System>` (`BaseUrl`, `ApiKey` (secret), `RequestTimeoutSeconds`). Mønster: full/light.

## Metoder
| Metode | Endpoint | Beskrivelse |
|---|---|---|
| `<Handling>Async(<Handling>RequestDto request)` | `POST <sti>` | … |

## Faldgruber
- …
````

## Tjekliste: ny service

1. Mappe `services/<System>/` med `Documents/`, `Dtos/` og `Interfaces/`.
2. Vælg full eller light pattern, og opret filerne fra skabelonen.
3. Gem det eksterne API's spec i `Documents/`, hvis den findes, og skriv `Documents/<System>.md`.
4. Sektionen `<System>` i `appsettings.json` med tomme værdier. Rigtige værdier i `appsettings.Development.json` (se `configuration.md`).
5. Registrering i `Program.cs` under `// Eksterne services`.
6. `<System>Exception` og en `case` for den i `GlobalExceptionHandler` (alle mønstre, se `errorhandling.md`).
7. Opdatér `<projekt>/.claude/CLAUDE.md` (Services og Konfiguration). Docker: tilføj nøglerne til `.env.example`, hvis den findes.

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
