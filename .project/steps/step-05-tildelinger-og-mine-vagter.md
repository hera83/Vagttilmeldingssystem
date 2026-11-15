# Step 05 - Tildelinger: forhåndstildeling, fjernelse og Mine vagter

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 02, Step 04 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Bygger tildelingerne og den race-sikre bookingkerne, som både admin (her) og forældre (step 07) bruger. Admin kan tildele kladdevagter i det skjulte og fjerne en bruger fra en vagt. Brugeren ser sine egne vagter, også de skjulte, der er tildelt vedkommende. Krav: K09, K11, K13, K15, K16, K17, K07.

## Omfang
- **Med:**
  - Tabellen `Assignments`: `ShiftId` (FK `Restrict`), `UserId` (FK `Restrict`), `Source` (`Admin`/`Self`), `IsActive`, `AssignedByUserId?`, `CancelledAt?`, `CancelledByUserId?`. Delvist unikt index på (`ShiftId`, `UserId`) `WHERE IsActive = 1`. Rækker slettes aldrig, de afmeldes (soft cancel), så historikken bevares.
  - **Bookingkernen** i `repositories/Assignments/`: `CreateManyAsync(userId, shiftIds, source, assignedBy, kunÅbne)` i én SQLite-transaktion (`BEGIN IMMEDIATE`): sortér og fjern dubletter, læs vagterne og antal aktive tildelinger, tjek status, kapacitet og om brugeren allerede er på. Alt eller intet. Returnerer et resultat (`Success`, `NotFound`, `NotOpen`, `Full`, `AlreadyAssigned`) med de vagt-id'er, der fejlede.
  - `CancelAsync(assignmentId, cancelledBy)`: soft cancel. Pladsen er ledig igen med det samme.
  - `managers/Assignments/` (`AssignmentManager`): admin-tildeling og -fjernelse (kombinerer Shifts, Users/Profiles og Assignments).
  - Admin: siden `Shifts/Assign/{id}` med søgning efter bruger (navn eller e-mail), "Tildel" (også til sig selv), listen over tildelte med **Fjern** (POST + bekræftelse). Virker for både kladder og åbne vagter. "Besat x/y" i vagttabellen på `Events/Details`.
  - Regler, der først kan laves nu: en vagt med aktive tildelinger kan ikke slettes, og kapaciteten kan ikke sænkes under antal aktive. En bruger med vagter kan ikke slettes i **Brugere** (besked: "Deaktivér brugeren i stedet").
  - `MyShiftsController` (**Mine vagter**, alle indloggede): kommende og afholdte vagter, månedskalender (server-renderet) og vagtdetaljer efter mockup 06. Oprindelse vises ("Tildelt af administrator" / "Selv tilmeldt"), og at vagten er låst ("Kontakt administratoren, hvis du bliver forhindret"). Kun egne aktive tildelinger, aldrig andre vagter fra samme begivenhed.
  - **Ingen** afmeld-, byt- eller giv videre-funktion for brugere.
- **Ikke med:** Brugernes egen tilmelding (step 07), realtid ved fjernelse (step 08), notifikationer (step 10–12).

## Plan
1. [ ] Model `Assignment` + enum `AssignmentSource`, `AssignmentConfiguration` (delvist unikt index med `HasFilter`), `DbSet` og migration `AddAssignmentsTable`.
2. [ ] Bookingkernen i `AssignmentRepository` med transaktion. Verificér med en test, at transaktionen faktisk er `BEGIN IMMEDIATE` (ellers sættes den eksplicit). Skriv transaktionsgrænsen i `Documents/Assignments.md`.
3. [ ] `AssignmentManager` + `Documents/Assignments.md` (manager).
4. [ ] `Shifts/Assign` med søgning, tildel og fjern. "Besat"-tal på `Events/Details`.
5. [ ] Spær sletning af vagter med aktive tildelinger og kapacitet under antal aktive. Spær sletning af brugere med tildelinger.
6. [ ] `MyShiftsController` + `viewmodels/MyShifts/` + `views/MyShifts/` (Index med liste og kalender, Details). Menupunktet **Mine vagter**.
7. [ ] Samtidighedstests mod en rigtig SQLite-fil med én `DbContext` pr. opgave: 50 parallelle tildelinger til forskellige brugere på en vagt med 1 plads giver præcis 1. Kapacitet 2 giver præcis 2. Dobbelt tildeling af samme bruger giver `AlreadyAssigned`.
8. [ ] Adgangstests: bruger A ser sin skjulte tildelte vagt, bruger B ser den ikke, og B får 404 på `MyShifts/Details/{id}` for A's vagt. En bruger uden admin får "Ingen adgang" på `Shifts/Assign`. Der findes ingen bruger-action, der afmelder (POST til et gættet endpoint giver 404).
9. [ ] Opdatér `web/.claude/CLAUDE.md`: Repositories, Managers, Controllers og endpoints. Notér, at `AssignmentRepository` læser `Shifts` i samme transaktion (bevidst, for at gøre kapacitetstjekket atomart).
10. [ ] Opdatér manualen: **Tildel en vagt**, **Fjern en bruger fra en vagt** og **Mine vagter**, skærmbilleder (eller Manglende billeder), ændringslog og `.manuals/README.md`.
11. [ ] Opdatér Status for K09/K11/K13/K15/K16/K17 i `.project/krav/krav.md`.
12. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 05 - Tildelinger: forhåndstildeling, fjernelse og Mine vagter" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/data/Models/Assignment.cs`, `web/data/Models/AssignmentSource.cs`, `web/data/Configurations/AssignmentConfiguration.cs`, `web/data/WebDbContext.cs`, `web/data/Migrations/` (ny/ændret)
- `web/repositories/Assignments/`, `web/managers/Assignments/` (nye)
- `web/repositories/Shifts/`, `web/repositories/Users/` (ændret: spærreregler)
- `web/controllers/MyShiftsController.cs`, `web/viewmodels/MyShifts/`, `web/views/MyShifts/` (nye)
- `web/controllers/ShiftsController.cs`, `web/controllers/UsersController.cs`, `web/views/Shifts/`, `web/views/Events/Details.cshtml`, `web/views/Shared/_Layout.cshtml` (ændret)
- `web.Tests/` (samtidigheds- og adgangstests)

## Beslutninger
- Kapaciteten beskyttes af SQLite's ene skriver (`BEGIN IMMEDIATE`) + det delvist unikke index. Ingen `lock` i C#, så det også virker, hvis flere processer deler databasen (foreslået af Claude).
- Tildelinger afmeldes og slettes aldrig. Det giver historik uden en audit-tabel (foreslået af Claude).
- Admin-tildelinger tæller med i kapaciteten, og admin kan ikke overbooke (krav-pakken, STEP_07).
- Påmindelser sendes også for tildelte kladdevagter, fordi brugeren kan se dem (afgøres endeligt i step 12).

## Test
- `dotnet build` og `dotnet test` (inkl. samtidighedstestene).
- Manuelt med to browsere: admin tildeler en kladdevagt til bruger A. A ser den under **Mine vagter** med "Tildelt af administrator". B ser intet. Admin fjerner A, og vagten forsvinder hos A.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
