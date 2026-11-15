# Step 03 - Begivenheder (admin)

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 01 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Administratoren kan oprette, rette, arkivere og finde begivenheder (fx "Skolefest" eller "Arbejdslørdag"), som vagterne senere hører under. Krav: K06 (begivenheder).

## Omfang
- **Med:**
  - Tabellen `Events`: `Name`, `Description`, `Location`, `StartDate`, `EndDate` (`DateOnly`, dansk dato), `IsArchived`.
  - `repositories/Events/`.
  - `EventsController` med `[Authorize(Policy = AuthorizationPolicies.AdminOnly)]`: `Index` (søgning, filter Aktive/Arkiverede), `Create`, `Edit`, `Details` (vagterne kommer i step 04), `Archive`/`Unarchive` (POST) og `Delete` (POST med bekræftelse).
  - Layout efter mockup 03: begivenhedsliste som kort/tabel, tom tilstand ("Der er ingen begivenheder endnu") og menupunktet **Begivenheder** i admin-menuen.
- **Ikke med:** Vagter (step 04). Reglen om, at en begivenhed med vagter ikke kan slettes, kommer i step 04, hvor tabellen `Shifts` findes.

## Plan
1. [ ] Model `Event`, `EventConfiguration`, `DbSet` og migration `AddEventsTable`.
2. [ ] `repositories/Events/` med Dtos, Interfaces, Mappers og `Documents/Events.md`.
3. [ ] `EventsController` + `viewmodels/Events/` + `views/Events/` (Index, Create, Edit, Details). Validering: navn påkrævet, slutdato ≥ startdato.
4. [ ] Menupunktet **Begivenheder** i admin-menuen.
5. [ ] Tests: admin kan oprette, rette, arkivere og finde. En bruger uden admin får "Ingen adgang" på alle actions, også POST. Slutdato før startdato afvises.
6. [ ] Opdatér `web/.claude/CLAUDE.md`: Repositories, Controllers og endpoints.
7. [ ] Opdatér manualen: afsnittet **Begivenheder** med trin og skærmbilleder (eller Manglende billeder), ændringslog og `.manuals/README.md`.
8. [ ] Opdatér Status for K06 i `.project/krav/krav.md` (delvist, vagterne mangler).
9. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 03 - Begivenheder (admin)" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/data/Models/Event.cs`, `web/data/Configurations/EventConfiguration.cs`, `web/data/WebDbContext.cs`, `web/data/Migrations/` (ny/ændret)
- `web/repositories/Events/` (ny)
- `web/controllers/EventsController.cs`, `web/viewmodels/Events/`, `web/views/Events/` (nye)
- `web/views/Shared/_Layout.cshtml`, `web/Program.cs` (ændret)

## Beslutninger
- En begivenhed har en periode med datoer (`DateOnly`), ikke klokkeslæt. Klokkeslæt hører til vagterne (foreslået af Claude).
- Begivenheder arkiveres i stedet for at blive slettet, når de har historik (foreslået af Claude, ChatGPT-pakken STEP_05).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt: opret "Skolefest", ret den, søg, arkivér og find den under Arkiverede.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
