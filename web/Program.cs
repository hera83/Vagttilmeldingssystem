using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using web.BgWorkers;
using web.Data;
using web.Data.Seeds;
using web.Infrastructure.Authentication;
using web.Infrastructure.ErrorHandling;
using web.Infrastructure.Logging;
using web.Managers.Members;
using web.Managers.Members.Interfaces;
using web.Managers.Planning;
using web.Managers.Planning.Interfaces;
using web.Repositories.Events;
using web.Repositories.Events.Interfaces;
using web.Repositories.Logs;
using web.Repositories.Logs.Interfaces;
using web.Repositories.Profiles;
using web.Repositories.Profiles.Interfaces;
using web.Repositories.Shifts;
using web.Repositories.Shifts.Interfaces;
using web.Repositories.Users;
using web.Repositories.Users.Dtos;
using web.Repositories.Users.Interfaces;

// Formularer fra <input type="date|time|number"> sender altid invariant format (2026-05-16, 17:00), uanset serverens sprog.
// Al visning bruger DateTimeExtensions.Danish eksplicit (projektets afvigelse, se web/.claude/CLAUDE.md).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

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

// Views i views/ (små bogstaver) og anti-forgery på alle formularer (views.md)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

    // Danske tekster, når en værdi slet ikke kan læses (fx bogstaver i et talfelt). controllers.md regel 32.
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"Værdien »{value}« er ikke gyldig for {field}.");
    messages.SetMissingBindRequiredValueAccessor(field => $"Der mangler en værdi for {field}.");
    messages.SetMissingKeyOrValueAccessor(() => "Der mangler en værdi.");
    messages.SetMissingRequestBodyRequiredValueAccessor(() => "Der mangler indhold i forespørgslen.");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"Værdien »{value}« er ikke gyldig.");
    messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Værdien er ikke gyldig.");
    messages.SetNonPropertyValueMustBeANumberAccessor(() => "Feltet skal være et tal.");
    messages.SetUnknownValueIsInvalidAccessor(field => $"Værdien er ikke gyldig for {field}.");
    messages.SetValueIsInvalidAccessor(value => $"Værdien »{value}« er ikke gyldig.");
    messages.SetValueMustBeANumberAccessor(field => $"{field} skal være et tal.");
    messages.SetValueMustNotBeNullAccessor(_ => "Feltet skal udfyldes.");
});
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/views/{1}/{0}" + RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/views/Shared/{0}" + RazorViewEngine.ViewExtension);
});

// Dansk UI: æ, ø og å skrives som tegn i HTML i stedet for &#xE6; osv. (projektets afvigelse, se web/.claude/CLAUDE.md)
builder.Services.Configure<WebEncoderOptions>(options =>
    options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHttpClient();

// app.db (dbcontext.md). Data Source er relativ i appsettings og gøres absolut ud fra ContentRootPath.
var appDb = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Default") ?? "Data Source=app_dbs/app.db");
appDb.DataSource = Path.Combine(builder.Environment.ContentRootPath, appDb.DataSource);
Directory.CreateDirectory(Path.GetDirectoryName(appDb.DataSource)!);
builder.Services.AddDbContext<WebDbContext>(options => options.UseSqlite(appDb.ToString()));

// Authentication (authentication.md): cookie-login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "web.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.EventsType = typeof(UserCookieEvents);
    });

builder.Services.AddScoped<UserCookieEvents>();

builder.Services.AddAuthorization(options =>
{
    // Alt kræver login, medmindre det har [AllowAnonymous].
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(UserClaimTypes.AdminRole));
});

// Repositories
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();

// Managers
builder.Services.AddScoped<IMemberManager, MemberManager>();
builder.Services.AddScoped<IPlanningManager, PlanningManager>();

// Eksterne services

// Workers
builder.Services.AddHostedService<LogCleanupWorker>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WebDbContext>();
    await dbContext.Database.MigrateAsync();
    await DataSeeder.SeedAsync(dbContext);

    // Første administrator fra Users:InitialAdmin, kun når tabellen Users er tom (cookieauth.md)
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

// Websider får fejlsiden. GlobalExceptionHandler logger, så middlewaren skal ikke logge en gang til.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandlingPath = "/Home/Error",
    SuppressDiagnosticsCallback = _ => true
});

// Tomme 4xx/5xx-svar viser statussiden
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Statiske filer fra wwwroot/ (anonymt, så login-siden har sit css) og standard-routen
app.MapStaticAssets().AllowAnonymous();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
