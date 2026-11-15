# Repositories (`repositories/`)

**Gælder for:** `repositories/` i alle projekter, al kode der læser og skriver i projektets egen database.
**Se også:** `dbcontext.md` (entities og `<Projekt>DbContext`), `files.md` (filer hører til et repository), `controllers.md` (hvem kalder repositories), `services.md` (eksterne systemer).

## Formål

Et repository er det eneste sted, der taler med projektets egne databaser i `app_dbs/`: `app.db` via EF Core og andre databasefiler (som standardens `log.db`) via ADO.NET. Alt udenfor arbejder med repositoriets Dtos og ved intet om EF Core eller SQL.

## Struktur

```text
<projekt>/repositories/
└── <Entiteter>/                       ← entiteten i flertal = DbSet-navnet   (namespace <projekt>.Repositories.<Entiteter>)
    ├── <Entitet>Repository.cs
    ├── Documents/
    │   └── <Entiteter>.md             ← AI-beskrivelse, se skabelon nedenfor
    ├── Dtos/                          ← repositoriets ind- og ud-modeller
    │   ├── <Entitet>Dto.cs
    │   ├── Create<Entitet>Dto.cs
    │   └── Update<Entitet>Dto.cs
    ├── Interfaces/
    │   └── I<Entitet>Repository.cs
    └── Mappers/
        └── <Entitet>Mapper.cs
```

## Regler

### Ansvar
1. `repositories/` håndterer projektets **egen** database og de filer i `app_files/`, der hører til en tabel (se `files.md`). Eksterne systemer hører til i `services/`.
2. **Al** adgang til `<Projekt>DbContext` sker gennem et repository. Controllers, services, bgworkers og andre klasser injicerer aldrig `<Projekt>DbContext` direkte. Undtagelser: opstartskoden i `Program.cs` (migrations + `DataSeeder`) og `ISeeder`-klasserne.
3. Før du opretter et nyt repository eller en ny metode, så læs `repositories/*/Documents/*.md` og tjek, om noget eksisterende allerede dækker behovet.

### Navngivning
4. Mappen hedder entiteten i **flertal** (`<Entiteter>/`), og klassen hedder `<Entitet>Repository` i ental. Interfacet hedder `I<Entitet>Repository`.
   *Hvorfor:* et namespace må aldrig hedde det samme som en klasse. En mappe `<Entitet>/` ville skygge for entiteten, og `<Entitet>Repository/` for repository-klassen. Begge dele giver compile-fejlen CS0118.
5. Namespaces følger mapperne: `<projekt>.Repositories.<Entiteter>` med `.Dtos`, `.Interfaces` og `.Mappers`.
6. Alle fire undermapper oprettes altid. Én klasse pr. fil.

### Metoder
7. Alle public metoder er `async`, ender på `Async` og tager `CancellationToken cancellationToken = default` som sidste parameter.
8. Metoder tager og returnerer **kun** repositoriets egne Dtos eller simple typer (`Guid`, `bool`, `int` …). EF-entities forlader aldrig repositoriet.
9. "Ikke fundet" kaster ikke en exception. Returnér `null` ved hentning og `false` ved opdatering/sletning. Skal kalderen bruge den opdaterede række (fx i svaret), så returnerer opdateringen `<Entitet>Dto?`, hvor `null` betyder "ikke fundet". Controlleren oversætter det til `NotFound()`.
10. **Læsning:** brug `AsNoTracking()` og projektion med mapperens expression (`.Select(<Entitet>Mapper.ToDtoExpression)`), så der kun hentes de kolonner, Dto'en bruger.
11. **Skrivning:** hver public skrive-metode er en afsluttet enhed og kalder selv `SaveChangesAsync`. Skal en handling skrive atomart på tværs af flere repositories, så spørg brugeren, før du indfører en transaktion.

### Dtos og Mappers
12. Dtos: `<Entitet>Dto` til læsning samt `Create<Entitet>Dto` og `Update<Entitet>Dto` til skrivning, og flere efter behov. Rene klasser, hvor strings får `= string.Empty` og samlinger `= []`.
13. Mappers skrives i hånden som `public static class <Entitet>Mapper`. Brug aldrig AutoMapper/Mapster. En mapper indeholder:
    - `ToDtoExpression` til EF-projektion
    - `ToDto(this <Entitet>)` til objekter, der allerede er i hukommelsen (genbruger expression'en)
    - `ToEntity(this Create<Entitet>Dto)` og `ApplyTo(this Update<Entitet>Dto, <Entitet>)`
    - Et repository uden EF Core (som standardens `repositories/Logs/`, der læser `log.db` med ADO.NET) har i stedet `ToDto(this SqliteDataReader)` og en konstant med de kolonner, metoden læser.

### Documents og registrering
14. `Documents/<Entiteter>.md` oprettes sammen med repositoriet og opdateres **i samme ændring**, hver gang en public metode tilføjes, ændres eller fjernes.
15. Registrér i `Program.cs` under `// Repositories`: `builder.Services.AddScoped<I<Entitet>Repository, <Entitet>Repository>();` (scoped ligesom `<Projekt>DbContext`).
16. Opdatér tabellen Repositories i `<projekt>/.claude/CLAUDE.md` i samme ændring.

## Skabelon: mapper + repository

```csharp
// Mappers/<Entitet>Mapper.cs
public static class <Entitet>Mapper
{
    public static readonly Expression<Func<<Entitet>, <Entitet>Dto>> ToDtoExpression = x => new <Entitet>Dto
    {
        Id = x.Id
        // … øvrige felter
    };

    private static readonly Func<<Entitet>, <Entitet>Dto> CompiledToDto = ToDtoExpression.Compile();

    public static <Entitet>Dto ToDto(this <Entitet> entity) => CompiledToDto(entity);

    public static <Entitet> ToEntity(this Create<Entitet>Dto dto) => new()
    {
        // … felter fra dto
    };

    public static void ApplyTo(this Update<Entitet>Dto dto, <Entitet> entity)
    {
        // entity.<Felt> = dto.<Felt>;
    }
}

// <Entitet>Repository.cs
public class <Entitet>Repository(<Projekt>DbContext dbContext) : I<Entitet>Repository
{
    public Task<<Entitet>Dto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.<Entiteter>.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(<Entitet>Mapper.ToDtoExpression)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<<Entitet>Dto> CreateAsync(Create<Entitet>Dto dto, CancellationToken cancellationToken = default)
    {
        var entity = dto.ToEntity();
        dbContext.<Entiteter>.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<<Entitet>Dto?> UpdateAsync(Guid id, Update<Entitet>Dto dto, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.<Entiteter>.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        dto.ApplyTo(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.<Entiteter>.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
```

## Skabelon: `Documents/<Entiteter>.md`

````markdown
# <Entiteter> (repository)

**Formål:** …
**Brug det når:** opgaven handler om <entiteter> i appens egen database.
**Brug det ikke til:** data fra eksterne systemer (det er en service).
**Tabeller/filer:** `<Entiteter>` (+ filer i `app_files/<Opgave>/`, hvis der er nogen)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetByIdAsync(Guid id)` | Henter én række, eller `null` hvis den ikke findes. |

## Faldgruber
- …
````

## Kendte afvigelser i koden

Ingen. Projektets nuværende repositories står i `<projekt>/.claude/CLAUDE.md`.
