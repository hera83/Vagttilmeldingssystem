# Datalag (`data/`)

**Gælder for:** `data/` i alle projekter, dvs. EF Core-opsætningen af projektets `app.db`.
**Se også:** `databases.md` (hvor databaser ligger), `repositories.md` (al adgang til `<Projekt>DbContext` sker via repositories), `commands.md` (dotnet ef).

## Struktur

`data/` har præcis disse 4 undermapper, og `<Projekt>DbContext.cs` ligger i roden af `data/`. DbContext hedder altid `<Projekt>DbContext`:

```text
<projekt>/data/
├── <Projekt>DbContext.cs                ← DbContext: DbSets + SaveChangesAsync-override   (namespace <projekt>.Data)
├── Configurations/                      ← én IEntityTypeConfiguration<T> pr. tabel   (namespace <projekt>.Data.Configurations)
│   └── <Entitet>Configuration.cs
├── Migrations/                          ← genereres af dotnet ef, redigeres aldrig i hånden
├── Models/                              ← én klasse pr. tabel + EntityBase.cs         (namespace <projekt>.Data.Models)
│   └── <Entitet>.cs
└── Seeds/                               ← ISeeder.cs, DataSeeder.cs + én seeder pr. tabel/formål   (namespace <projekt>.Data.Seeds)
    └── <Entitet>Seeder.cs
```

## Regler

### Models (tabeller)
1. Én klasse pr. fil. Klassen hedder entitetens navn i ental (`<Entitet>`), og `DbSet` hedder flertal (`<Entiteter>`).
2. Alle modeller arver `EntityBase`: `Guid Id` (UUIDv7 via `Guid.CreateVersion7()`), `DateTime CreatedAt` og `DateTime UpdatedAt`. Tidsstemplerne er UTC og sættes **automatisk** i `<Projekt>DbContext.SaveChangesAsync`, så de skal aldrig sættes i hånden.
3. Ikke-nullable strings får `= string.Empty`, og samlinger får `= []`. Kun reelt valgfrie kolonner er nullable.
4. Modeller er rene klasser uden attributter. Alle constraints (max-længde, index, unikhed, relationer) ligger i modellens Configuration.

### Configurations
5. Én fil pr. tabel: `data/Configurations/<Entitet>Configuration.cs`, som implementerer `IEntityTypeConfiguration<<Entitet>>`.
6. `<Projekt>DbContext.OnModelCreating` indeholder kun `modelBuilder.ApplyConfigurationsFromAssembly(typeof(<Projekt>DbContext).Assembly);`. Skriv aldrig tabelkonfiguration direkte i `OnModelCreating`.
7. Sæt altid `HasMaxLength` på string-kolonner.

### Migrations
8. Opret migrations fra projektmappen `<projekt>/`: `dotnet ef migrations add <Navn> --output-dir data/Migrations --context <Projekt>DbContext`. `<Navn>` er PascalCase og beskriver ændringen, fx `Add<Entiteter>Table` eller `Add<Felt>To<Entiteter>`.
9. Redigér aldrig genererede migration- eller snapshot-filer. Er en migration forkert og endnu ikke deployet, så fjern den med `dotnet ef migrations remove --context <Projekt>DbContext` og generér den igen.
10. Migrations køres automatisk ved opstart (`MigrateAsync()` i `Program.cs`).
11. EF genererer namespace `<projekt>.data.Migrations` (lille `d`, fordi mappen hedder `data`). Det er accepteret og den eneste undtagelse fra PascalCase-namespaces. Brug ikke `--namespace`.

### Seeds
12. Én `ISeeder`-klasse pr. tabel/formål, registreret i `Seeders`-arrayet i `DataSeeder.cs`. De kører ved **hver** opstart, efter migrations, i **alle** miljøer.
13. Seeders skal være idempotente: tjek, om data allerede findes, før der indsættes, så de kan køre igen og igen uden dubletter.
14. Seed kun data, der hører til i alle miljøer (fx opslagsværdier). Seed aldrig test- eller demodata, medmindre brugeren beder om det.
15. Seedede rækker, som anden kode henviser til, får faste Guid-værdier (`new Guid("...")`), så id'et er det samme i alle miljøer.
16. Den første administrator i web-delen er ikke en seeder. Den kræver konfiguration og en secret og oprettes derfor i `Program.cs` fra `Users:InitialAdmin` (se `program.md` og `cookieauth.md`).

## Standardtabeller

| Tabel (`DbSet`) | Findes i | Instruks |
|---|---|---|
| `ApiKeys` (`ApiKey`) | API-delen | `apikeys.md` |
| `Users` (`User`) | Web-delen | `cookieauth.md` |

Et nyt projekt får standardtabellerne for sin type og en første migration (fx `InitialCreate`), før appen startes første gang.

## Skabeloner

```csharp
// data/Models/EntityBase.cs
public abstract class EntityBase
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// data/Models/<Entitet>.cs
public class <Entitet> : EntityBase
{
    public string Name { get; set; } = string.Empty;
}

// data/<Projekt>DbContext.cs
public class <Projekt>DbContext(DbContextOptions<<Projekt>DbContext> options) : DbContext(options)
{
    public DbSet<<Entitet>> <Entiteter> => Set<<Entitet>>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(<Projekt>DbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<EntityBase>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified) entry.Entity.UpdatedAt = now;
        }
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}

// data/Configurations/<Entitet>Configuration.cs
public class <Entitet>Configuration : IEntityTypeConfiguration<<Entitet>>
{
    public void Configure(EntityTypeBuilder<<Entitet>> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

// data/Seeds/ISeeder.cs
public interface ISeeder
{
    Task SeedAsync(<Projekt>DbContext context, CancellationToken cancellationToken = default);
}

// data/Seeds/DataSeeder.cs
public static class DataSeeder
{
    private static readonly ISeeder[] Seeders = [new <Entitet>Seeder()];

    public static async Task SeedAsync(<Projekt>DbContext context, CancellationToken cancellationToken = default)
    {
        foreach (var seeder in Seeders)
        {
            await seeder.SeedAsync(context, cancellationToken);
        }
    }
}

// data/Seeds/<Entitet>Seeder.cs
public class <Entitet>Seeder : ISeeder
{
    public async Task SeedAsync(<Projekt>DbContext context, CancellationToken cancellationToken = default)
    {
        if (await context.<Entiteter>.AnyAsync(cancellationToken))
        {
            return;
        }

        context.<Entiteter>.Add(new <Entitet> { Id = new Guid("00000000-0000-0000-0000-000000000001"), Name = "…" });
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

## Tjekliste: ny tabel

1. Model i `data/Models/<Entitet>.cs`, som arver `EntityBase`.
2. Configuration i `data/Configurations/<Entitet>Configuration.cs`.
3. `public DbSet<<Entitet>> <Entiteter> => Set<<Entitet>>();` i `<Projekt>DbContext`.
4. Migration (regel 8).
5. Repository (se `repositories.md`).
6. Seeder, hvis tabellen skal have startdata (regel 12-15).
7. Opdatér tabellen Repositories (og evt. Databaser) i `<projekt>/.claude/CLAUDE.md`.

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
