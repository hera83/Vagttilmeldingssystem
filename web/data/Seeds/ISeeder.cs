namespace web.Data.Seeds;

public interface ISeeder
{
    Task SeedAsync(WebDbContext context, CancellationToken cancellationToken = default);
}
