# Extensions og helpers (`extensions/`)

**Gælder for:** `extensions/` i alle projekter, generelle extension-metoder og statiske hjælpefunktioner.
**Se også:** `infrastructure.md` (opsætningskode), `controllers.md` og `repositories.md` (mappers ligger **ikke** her).

## Formål

Små, genbrugelige funktioner, som kan bruges overalt i projektet uden opsætning.

## Struktur

```text
<projekt>/extensions/                ← namespace <projekt>.Extensions (flad mappe)
├── <Type>Extensions.cs              ← extension-metoder til én type, fx StringExtensions for string
└── <Emne>Helper.cs                  ← statiske hjælpere, der ikke er extensions
```

## Regler

1. Alt i `extensions/` er `public static class` med `static` metoder.
2. **Extension-metoder** grupperes efter den type, de udvider: `<Type>Extensions` (`StringExtensions` for `string`, `DateTimeExtensions` for `DateTime`). Én klasse pr. type.
3. **Helpers**, der ikke er extensions, hedder `<Emne>Helper`.
4. Funktionerne skal være **rene**: ingen dependency injection, ingen tilstand (statiske felter med data), ingen I/O (database, HTTP, filsystem) og ingen konfiguration. Kræver en funktion noget af det, så er den en service, et repository eller infrastruktur.
5. Kun **generelle** funktioner hører til her. Hjælpere, der kun giver mening for én feature, bliver i featurens mappe (som standardens `ApiKeyGenerator`, der ligger i `infrastructure/Authentication/`).
6. Mappers hører **ikke** til her. De ligger i `dtos/<Navn>/` (controllere) og `repositories/<Entiteter>/Mappers/`.
7. Service-registreringer (`AddScoped` osv.) hører ikke til her. De står direkte i `Program.cs`.
8. Før du skriver en ny hjælpefunktion, så tjek, om `extensions/` allerede har en, der kan bruges eller udvides.
9. Mappen er flad: ingen undermapper.

## Skabelon

```csharp
namespace <projekt>.Extensions;

public static class <Type>Extensions
{
    public static <Returtype> <Metode>(this <Type> value) => …;
}
```

## Kendte afvigelser i koden

Ingen. Findes mappen ikke i et projekt, så opret den sammen med den første extension.
