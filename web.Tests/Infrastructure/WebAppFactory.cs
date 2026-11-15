using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using web.Repositories.Users.Dtos;
using web.Repositories.Users.Interfaces;

namespace web.Tests.Infrastructure;

/// <summary>
/// Kører appen i hukommelsen mod en midlertidig mappe (content root), så app.db og log.db
/// aldrig rører udviklingsdatabasen i web/app_dbs/. Hver fabrik har sin egen database.
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>
{
    public const string AdminUserName = "admin@test.local";
    public const string AdminPassword = "Admin-Test-Password-123";
    public const string UserPassword = "User-Test-Password-123";

    public string RootPath { get; } = Path.Combine(Path.GetTempPath(), "vagttilmelding-tests", Guid.NewGuid().ToString("N"));

    public string DatabasePath => Path.Combine(RootPath, "app_dbs", "app.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(RootPath);
        builder.UseEnvironment("Development");
        builder.UseContentRoot(RootPath);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={DatabasePath}");
        builder.UseSetting("Users:InitialAdmin:UserName", AdminUserName);
        builder.UseSetting("Users:InitialAdmin:Password", AdminPassword);
        ConfigureTestServices(builder);
    }

    /// <summary>Senere tests kan udskifte services (fx falske mail- og push-services).</summary>
    protected virtual void ConfigureTestServices(IWebHostBuilder builder)
    {
    }

    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public async Task<UserDto> CreateUserAsync(string userName, bool isAdmin = false, string? password = null, string? displayName = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        return await users.CreateAsync(new CreateUserDto
        {
            UserName = userName,
            DisplayName = displayName ?? userName,
            Password = password ?? UserPassword,
            IsAdmin = isAdmin
        }) ?? throw new InvalidOperationException($"Brugeren {userName} findes allerede.");
    }

    public async Task<T> WithServiceAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            Directory.Delete(RootPath, recursive: true);
        }
        catch (IOException)
        {
            // Filerne kan være låst et øjeblik endnu. De ligger i temp og må gerne blive.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
