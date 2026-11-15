namespace web.Data.Seeds;

public static class DataSeeder
{
    // Ingen seeders endnu. Projektet har ingen opslagsdata, der skal findes i alle miljøer.
    private static readonly ISeeder[] Seeders = [];

    public static async Task SeedAsync(WebDbContext context, CancellationToken cancellationToken = default)
    {
        foreach (var seeder in Seeders)
        {
            await seeder.SeedAsync(context, cancellationToken);
        }
    }
}
