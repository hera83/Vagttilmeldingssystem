# Managers (`managers/`)

**Gælder for:** `managers/` i alle projekter, forretningslogik, der kombinerer flere repositories og/eller services.
**Se også:** `controllers.md` og `bgworkers.md` (kalderne), `repositories.md` og `services.md` (det, managers kalder).

## Hvornår skal der bruges en manager? (hybrid-reglen)

| Situation | Her ligger logikken |
|---|---|
| Actionen/jobbet bruger **ét** repository **eller** **én** service | Direkte i controlleren/bgworkeren. Ingen manager. |
| Actionen/jobbet kombinerer **flere** repositories og/eller services | I en manager. |
| Samme logik bruges flere steder (fx både fra en controller og en bgworker) | I en manager. |

Vokser en simpel action ud over én afhængighed, så flyt logikken til en manager i samme ændring.

## Struktur

```text
<projekt>/managers/
└── <Område>/                       ← forretningsområdet i flertal (samme navn som repositoriets <Entiteter>/, når det giver mening)
    ├── <Område>Manager.cs          ← namespace <projekt>.Managers.<Område>
    ├── Documents/
    │   └── <Område>.md             ← AI-beskrivelse, skabelonen fra repositories.md
    ├── Dtos/                       ← managerens egne ind-/ud-modeller og resultat-enums (kun hvis der er nogen)
    └── Interfaces/
        └── I<Område>Manager.cs
```

Er `<Område>` det samme som en entitet, så brug entiteten i ental i klassenavnet (`<Entitet>Manager`) og flertal i mappen (`<Entiteter>/`), ligesom repositories. Mappe og klasse må aldrig hedde det samme.

## Regler

1. Navngivningen følger repositories: mappen er området, klassen hedder `<…>Manager`, og interfacet hedder `I<…>Manager`. Et namespace må aldrig hedde det samme som en klasse.
2. Managers injicerer repositories og services via deres interfaces. Aldrig `<Projekt>DbContext`, `HttpClient` eller andre lavniveau-typer.
3. Managers tager og returnerer repositoriers/servicers Dtos, egne Dtos eller simple typer. **Aldrig** controllerens Request/ResponseDtos eller ViewModels, så både API-controllere, web-controllere og bgworkers kan bruge dem.
4. Managers ved intet om HTTP: ingen `IActionResult`, `HttpContext`, `ModelState` eller statuskoder.
5. Forventede udfald (ikke fundet, findes allerede, ugyldig tilstand) returneres og kastes ikke: `null`/`false`, eller en `enum` (i `Dtos/`), når der er flere udfald. Controlleren oversætter dem til `NotFound()`, `Conflict()` osv. Exceptions er kun til uventede fejl.
6. Managers kalder ikke andre managers. Virker det nødvendigt, så spørg brugeren.
7. Alle public metoder er `async`, ender på `Async` og tager `CancellationToken cancellationToken = default` som sidste parameter.
8. Skal en manager skrive atomart på tværs af flere repositories, så spørg brugeren, før du indfører en transaktion.
9. Før du opretter en ny manager eller metode, så læs `managers/*/Documents/*.md`. Opdatér `Documents/` i samme ændring, som public metoder ændres.
10. Registrér i `Program.cs` under `// Managers`: `builder.Services.AddScoped<I<Område>Manager, <Område>Manager>();`
11. Opdatér tabellen Managers i `<projekt>/.claude/CLAUDE.md` i samme ændring.

## Skabelon

```csharp
// Udfald, når der er flere end "fundet/ikke fundet" (Dtos/<Handling>Result.cs)
public enum <Handling>Result
{
    Success,
    NotFound,
    Conflict
}

public class <Område>Manager(
    I<Entitet>Repository <entitet>Repository,
    I<System>Service <system>Service) : I<Område>Manager
{
    public async Task<<Handling>Result> <Handling>Async(Guid id, CancellationToken cancellationToken = default)
    {
        var <entitet> = await <entitet>Repository.GetByIdAsync(id, cancellationToken);
        if (<entitet> is null)
        {
            return <Handling>Result.NotFound;
        }

        // … kald service, gem via repository …
        return <Handling>Result.Success;
    }
}
```

`<Handling>` er det, metoden gør (fx `Approve`, `Sync`), og `<system>` er servicens navn i camelCase.

## Kendte afvigelser i koden

Ingen. Findes mappen ikke i et projekt, så opret den sammen med den første manager.
