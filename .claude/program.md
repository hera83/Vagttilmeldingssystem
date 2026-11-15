# `Program.cs` og pakker

**Gælder for:** `Program.cs` og `<projekt>.csproj` i alle projekter.
**Se også:** `project.md` (projekttyper og markeringerne `[API-del]`, `[Web-del]` …), `authentication.md`, `swagger.md`, `views.md`, `logging.md`, `errorhandling.md`.

## Regler

1. Alle registreringer står direkte i `Program.cs`, ikke i extension-metoder.
2. Registreringer i denne rækkefølge: `apiName` + badge-variabler (API-delen) → Serilog og log-database → controllers (+ views i web-delen) → ProblemDetails + `GlobalExceptionHandler` → OpenAPI (API-delen) → `AddHttpClient()` → `<Projekt>DbContext` → authentication + authorization → repositories → managers → eksterne services → workers.
3. Grupperne repositories, managers, eksterne services og workers står under hver sin kommentar (`// Repositories`, `// Managers`, `// Eksterne services`, `// Workers`). Nye registreringer tilføjes nederst i deres gruppe. Findes kommentaren ikke, så tilføj den på sin plads i rækkefølgen.
4. Ved opstart, før pipelinen, i ét scope: `MigrateAsync()` → `DataSeeder.SeedAsync()` → (web-delen) første administrator, hvis tabellen `Users` er tom (se `cookieauth.md`).
5. Pipeline i denne rækkefølge: `UseExceptionHandler` (altid først) → statussider → (API-delen) redirect til Swagger UI, `MapOpenApi`, `UseSwaggerUI` → `UseHttpsRedirection` → `UseRouting` → `UseAuthentication` → `UseAuthorization` → (web-delen) `MapStaticAssets` → `MapControllerRoute` (web-delen) eller `MapControllers` (kun API).
6. `app.UseRouting()` står altid eksplicit. Uden den finder statussidernes re-execute ingen route, og en tom 404/400 på en webside bliver ved med at være tom.
7. Brug kun blokkene for projektets type, og fjern markeringerne (se `project.md`).

## Pakker (`<projekt>.csproj`)

Brug samme versioner som de øvrige projekter i løsningen. Er der ingen, så brug den nyeste stabile version til `net10.0`, med én undtagelse: `Microsoft.OpenApi` skal ligge i det interval, som `Microsoft.AspNetCore.OpenApi` kræver. Til 10.0.x er det `>= 2.12.2` og `< 3.0.0`. Med `Microsoft.OpenApi` 3.x fejler buildet i den kode, `Microsoft.AspNetCore.OpenApi` genererer (`CS0200` på `IOpenApiMediaType.Example`).

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>

<ItemGroup>
  <!-- Alle projekter -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="..." />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="...">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="Serilog.AspNetCore" Version="..." />
  <PackageReference Include="Serilog.Sinks.SQLite" Version="..." />

  <!-- [API-del] -->
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="..." />
  <PackageReference Include="Microsoft.OpenApi" Version="2.x" />
  <PackageReference Include="Swashbuckle.AspNetCore.SwaggerUI" Version="..." />
</ItemGroup>
```

Web-delen skal ikke bruge flere pakker. `PasswordHasher<T>` til cookie-login ligger i ASP.NET Core's shared framework.

Afprøvet sammen (8. november 2026): `Microsoft.EntityFrameworkCore.Sqlite`/`.Design` 10.0.14, `Microsoft.AspNetCore.OpenApi` 10.0.14, `Microsoft.OpenApi` 2.13.0, `Swashbuckle.AspNetCore.SwaggerUI` 10.3.1, `Serilog.AspNetCore` 10.0.0, `Serilog.Sinks.SQLite` 7.0.0.

## Skabelon: `Program.cs`

```csharp
using <projekt>.BgWorkers;
using <projekt>.Data;
using <projekt>.Data.Seeds;
using <projekt>.Infrastructure.Authentication;
using <projekt>.Infrastructure.ErrorHandling;
using <projekt>.Infrastructure.Logging;
using <projekt>.Infrastructure.OpenApi;              // [API-del]
using <projekt>.Infrastructure.Routing;              // [Kombineret]
using <projekt>.Repositories.ApiKeys;                // [API-del]
using <projekt>.Repositories.ApiKeys.Interfaces;     // [API-del]
using <projekt>.Repositories.Logs;
using <projekt>.Repositories.Logs.Interfaces;
using <projekt>.Repositories.Users;                  // [Web-del]
using <projekt>.Repositories.Users.Dtos;             // [Web-del]
using <projekt>.Repositories.Users.Interfaces;       // [Web-del]
using Microsoft.AspNetCore.Authentication.Cookies;   // [Web-del]
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;                      // [Web-del]
using Microsoft.AspNetCore.Mvc.Razor;                // [Web-del]
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// [API-del] Navn og miljø-badge til OpenAPI og Swagger UI (swagger.md)
var apiName = builder.Configuration["Api:Name"] ?? "<projekt>";
var environmentName = builder.Environment.EnvironmentName;
var environmentBadgeColor = environmentName switch
{
    "Production" => "brightgreen",
    "Development" => "orange",
    _ => "yellow"
};
var environmentBadgeMarkdown =
    $"![{environmentName}](https://img.shields.io/badge/env-{Uri.EscapeDataString(environmentName)}-{environmentBadgeColor})";

// Serilog og log-database (logging.md)
var logDatabaseSettings = new LogDatabaseSettings(builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(logDatabaseSettings.DatabasePath)!);
builder.Services.AddSingleton(logDatabaseSettings);
builder.Host.UseSerilog((_, _, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.SQLite(
        sqliteDbPath: logDatabaseSettings.DatabasePath,
        tableName: LogDatabaseSettings.TableName,
        storeTimestampInUtc: true,
        maxDatabaseSize: LogDatabaseSettings.SinkMaxSizeMegabytes,
        rollOver: false));

// [Kun API]
builder.Services.AddControllers();

// [Web-del] Views i views/ (små bogstaver) og anti-forgery på alle formularer (views.md)
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/views/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/views/Shared/{0}" + RazorViewEngine.ViewExtension);
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// [API-del] OpenAPI (swagger.md)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = apiName;
        document.Info.Description = environmentBadgeMarkdown;
        document.Servers?.Clear();
        return Task.CompletedTask;
    });
});

builder.Services.AddHttpClient();

// app.db (dbcontext.md). Data Source er relativ i appsettings og gøres absolut ud fra ContentRootPath.
var appDb = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Default") ?? "Data Source=app_dbs/app.db");
appDb.DataSource = Path.Combine(builder.Environment.ContentRootPath, appDb.DataSource);
Directory.CreateDirectory(Path.GetDirectoryName(appDb.DataSource)!);
builder.Services.AddDbContext<<Projekt>DbContext>(options => options.UseSqlite(appDb.ToString()));

// Authentication (authentication.md). Præcis én af de tre blokke.
// [Kun API]
builder.Services.AddAuthentication(ApiKeyAuthenticationOptions.Scheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.Scheme, null);

// [Kun web]
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "<projekt>.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.EventsType = typeof(UserCookieEvents);
    });

// [Kombineret] Policy-scheme: x-api-key under /api, cookie-login alle andre steder
builder.Services.AddAuthentication(CombinedAuthentication.Scheme)
    .AddPolicyScheme(CombinedAuthentication.Scheme, CombinedAuthentication.Scheme,
        options => options.ForwardDefaultSelector = CombinedAuthentication.SelectScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.Scheme, null)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        // Samme indhold som AddCookie i [Kun web]-blokken ovenfor.
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "<projekt>.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.EventsType = typeof(UserCookieEvents);
    });

// [Web-del]
builder.Services.AddScoped<UserCookieEvents>();

builder.Services.AddAuthorization(options =>
{
    // Alt kræver login eller nøgle, medmindre det har [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // [API-del]
    options.AddPolicy(AuthorizationPolicies.MasterKeyOnly, policy =>
        policy.RequireClaim(ApiKeyClaimTypes.KeyType, ApiKeyClaimTypes.Master));

    // [Web-del]
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(UserClaimTypes.AdminRole));
});

// Repositories
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();   // [API-del]
builder.Services.AddScoped<IUserRepository, UserRepository>();       // [Web-del]

// Managers

// Eksterne services

// Workers
builder.Services.AddHostedService<LogCleanupWorker>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<<Projekt>DbContext>();
    await dbContext.Database.MigrateAsync();
    await DataSeeder.SeedAsync(dbContext);

    // [Web-del] Første administrator fra Users:InitialAdmin, kun når tabellen Users er tom (cookieauth.md)
    var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var initialAdminUserName = app.Configuration["Users:InitialAdmin:UserName"];
    var initialAdminPassword = app.Configuration["Users:InitialAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(initialAdminUserName) && !string.IsNullOrWhiteSpace(initialAdminPassword) &&
        !await userRepository.AnyAsync())
    {
        await userRepository.CreateAsync(new CreateUserDto
        {
            UserName = initialAdminUserName,
            DisplayName = initialAdminUserName,
            Password = initialAdminPassword,
            IsAdmin = true
        });
    }
}

// [Kun API] Exceptions og tomme 4xx/5xx-svar bliver til ProblemDetails (errorhandling.md)
app.UseExceptionHandler();
app.UseStatusCodePages();

// [Web-del] Websider får fejlsiden. GlobalExceptionHandler logger, så middlewaren skal ikke logge en gang til.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandlingPath = "/Home/Error",
    SuppressDiagnosticsCallback = _ => true
});

// [Kun web] Tomme 4xx/5xx-svar viser statussiden
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

// [Kombineret] Tomme 4xx/5xx-svar: ProblemDetails under /api og statusside på websider
app.UseWhen(ApiPath.IsApiRequest, api => api.UseStatusCodePages());
app.UseWhen(context => !ApiPath.IsApiRequest(context),
    web => web.UseStatusCodePagesWithReExecute("/Home/Status/{0}"));

// [Kun API] GET / -> Swagger UI. Middleware, ikke et endpoint (swagger.md).
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/" && HttpMethods.IsGet(context.Request.Method))
    {
        context.Response.Redirect("/swagger");
        return;
    }

    await next();
});
app.MapOpenApi().AllowAnonymous();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", apiName);
    options.DocumentTitle = apiName;
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    options.DefaultModelsExpandDepth(-1);
});

// [Kombineret] GET /api -> Swagger UI. OpenAPI og Swagger UI ligger under /api.
app.Use(async (context, next) =>
{
    if ((context.Request.Path == "/api" || context.Request.Path == "/api/") && HttpMethods.IsGet(context.Request.Method))
    {
        context.Response.Redirect("/api/swagger");
        return;
    }

    await next();
});
app.MapOpenApi("/api/openapi/{documentName}.json").AllowAnonymous();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";
    options.SwaggerEndpoint("/api/openapi/v1.json", apiName);
    options.DocumentTitle = apiName;
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    options.DefaultModelsExpandDepth(-1);
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// [Web-del] Statiske filer fra wwwroot/ (anonymt, så login-siden har sit css) og standard-routen
app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// [Kun API]
app.MapControllers();

app.Run();
```

`MapControllerRoute` mapper også attribute-routede controllere, så et kombineret projekt skal ikke kalde `MapControllers()`.

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
