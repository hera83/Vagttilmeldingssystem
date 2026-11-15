# Databaser (`app_dbs/`)

**Gælder for:** alle databasefiler, projektet ejer.
**Se også:** `dbcontext.md` (EF Core-opsætning), `repositories.md` (adgang til data), `files.md` (filer + metadata), `logging.md` (log.db).

## Formål

Alle databaser har ét fælles hjem: `app_dbs/` i det projekt, der ejer dem. Så er det altid tydeligt, hvor data ligger, hvad der skal tages backup af, og hvad der skal mountes som volume i Docker.

## Regler

1. Læg **alle** databasefiler i `app_dbs/` i roden af det projekt, der ejer dem (`<projekt>/app_dbs/`). Aldrig ved siden af featurens kode og aldrig i `bin/`.
2. SQLite er standard. Brug kun en anden databasemotor, hvis opgaven udtrykkeligt beder om det.
3. Nye tabeller lægges i projektets `app.db` via `<Projekt>DbContext` (se `dbcontext.md`). Opret kun en ny databasefil, hvis data har en helt anden livscyklus end appens øvrige data (som logs), og spørg brugeren først.
4. Byg stien til en databasefil ud fra `ContentRootPath`, aldrig ud fra den aktuelle arbejdsmappe, og sørg for, at mappen findes:
   ```csharp
   var dbPath = Path.Combine(builder.Environment.ContentRootPath, "app_dbs", "<navn>.db");
   Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
   ```
5. Connection strings ligger i `ConnectionStrings:<Navn>` i `appsettings.json` med en relativ `Data Source` (`app_dbs/<navn>.db`). `Program.cs` gør den absolut ud fra `ContentRootPath` med `SqliteConnectionStringBuilder` (regel 4).
6. **Filer og metadata:** Når en opgave gemmer filer, gemmes metadata (navn, type, størrelse, sti …) altid i databasen, og selve filen i `app_files/`. Detaljerne står i `files.md`.
7. Gem altid tidspunkter i UTC (`DateTime.UtcNow`).
8. SQLite kører i WAL-tilstand, så `<navn>.db-wal` og `<navn>.db-shm` hører til databasen. Flyt eller slet dem aldrig hver for sig.
9. Databasefiler må aldrig i git (se `gitignore.md`). Kun `app_dbs/.gitkeep` committes.

## Standard-databaser i et projekt

| Fil | Indhold | Skrives af | Læses via |
|---|---|---|---|
| `app.db` | Projektets egne tabeller (EF Core, `<Projekt>DbContext`) | Repositories (se `repositories.md`) | Repositories |
| `log.db` | Serilog-logs, tabel `Logs` | Kun Serilog og `LogCleanupWorker` | Et read-only repository (se `logging.md`) |

Hvilke databaser et projekt faktisk har, står i `<projekt>/.claude/CLAUDE.md`.

## Kendte afvigelser i koden

Ingen.
