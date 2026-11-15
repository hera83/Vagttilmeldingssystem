# Filer (`app_files/`)

**Gælder for:** alle filer, appen gemmer på disk: uploads fra brugere, filer hentet fra services/eksterne systemer, genererede eksporter osv.
**Se også:** `databases.md` (metadata), `repositories.md` (hvem skriver filen), `gitignore.md`, `docker.md` (volume).

## Formål

Al fil-lagring har ét fælles hjem, så filer aldrig ligger spredt rundt i projektet, og så det er tydeligt, hvad der skal tages backup af.

## Struktur

```text
<projekt>/app_files/
├── .gitkeep
└── <Opgave>/              ← én undermappe pr. opgave/datamodtager, PascalCase
    ├── .gitkeep
    └── <guid>.<endelse>   ← fx 3f2a9c1e5b7d4e0f9a8b6c5d4e3f2a1b.pdf
```

## Regler

1. Skal en funktion gemme filer, så gemmes de i `app_files/` i det aktive projekt (`<projekt>/app_files/`). Ingen andre steder.
2. Hver opgave/datamodtager får sin egen undermappe `<Opgave>/` med et sigende navn i PascalCase, typisk tabellens navn i flertal (`<Entiteter>/`). Læg en tom `.gitkeep` i nye undermapper (se `gitignore.md`).
3. Filen gemmes som `<guid>.<endelse>`: `Guid.NewGuid().ToString("N")` + den originale endelse med små bogstaver. Brug **aldrig** brugerens filnavn som (en del af) stien.
4. Metadata gemmes altid i en tabel i `app.db` (se `databases.md`) med mindst disse kolonner:

| Kolonne | Type | Indhold |
|---|---|---|
| `OriginalFileName` | `string` | Navnet, som brugeren/systemet leverede det. Bruges ved download. |
| `RelativePath` | `string` | Stien relativt til `app_files/`, fx `<Opgave>/3f2a….pdf`. **Aldrig** en absolut sti. |
| `ContentType` | `string` | MIME-type, fx `application/pdf`. |
| `SizeBytes` | `long` | Størrelse i bytes. |
| `CreatedAt` | `DateTime` | Tidspunkt i UTC (kommer fra `EntityBase`). |

5. Det repository, der ejer metadata-tabellen, står for **både** databaserækken og selve filen (se `repositories.md`):
   - **Gem:** skriv filen først, og gem derefter rækken. Fejler databasen, så slet filen igen.
   - **Slet:** slet rækken og filen i samme metode.
   - Der må aldrig ligge en fil uden række eller en række uden fil.
6. Byg den absolutte sti ud fra `ContentRootPath`: `Path.Combine(env.ContentRootPath, "app_files", relativePath)`. Opret mappen med `Directory.CreateDirectory`, før der skrives.
7. Arbejd med streams, og læs aldrig hele filen ind i hukommelsen: `await formFile.CopyToAsync(fileStream, cancellationToken)`.
8. Validér filtype og størrelse, før der skrives til disk.
9. Ved download sendes filen med det originale navn fra databasen: `PhysicalFile(absolutePath, contentType, originalFileName)`.
10. Reglerne gælder kun filer, der skal **persisteres**. Filer, der bare sendes videre (fx et svar fra en service, der streames direkte til klienten), gemmes ikke.
11. Skriv i `<projekt>/.claude/CLAUDE.md` (under Repositories), hvilke `app_files/`-undermapper projektet har.

## Kendte afvigelser i koden

Ingen. Projektspecifikke afvigelser står i `<projekt>/.claude/CLAUDE.md`.
