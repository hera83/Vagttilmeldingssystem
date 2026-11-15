# Step 04 - Vagter og kladder (admin)

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 03 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Administratoren kan oprette vagter med tidsrum og antal pladser under en begivenhed. Nye vagter er altid skjulte kladder. Krav: K06, K07, K10.

## Omfang
- **Med:**
  - Tabellen `Shifts`: `EventId` (FK, `Restrict`), `Title`, `Description`, `StartUtc`, `EndUtc`, `Capacity` (`CHECK` ≥ 1), `Status` (`Draft`/`Open`, gemt som tekst), `OpenedAt?`, `OpenedByUserId?`.
  - `repositories/Shifts/`. `CreateAsync` sætter altid `Status = Draft`. Status findes ikke i Create/Edit-ViewModels, så en formular ikke kan åbne en vagt.
  - `ShiftsController` (`AdminOnly`): `Create` (for en begivenhed), `Edit`, `Delete` (POST med bekræftelse).
  - `Events/Details` viser begivenhedens vagter som tabel efter mockup 03: vagt, tid, pladser, besat (0 indtil step 05), statusbadge **Skjult – kladde** (grå) eller **Åben for tilmelding** (grøn) med ikon og tekst, statusfilter.
  - Formular efter mockup 04 (venstre del + opsummering): dato, starttid, sluttid i dansk tid, antal pladser og beskrivelse.
  - Tidszone: admin indtaster dansk tid, der gemmes i UTC. Omregning i én ren helper i `extensions/` (`Europe/Copenhagen`). Ugyldige tider ved skift til sommertid afvises med en besked.
  - En begivenhed med vagter kan ikke slettes, kun arkiveres (tjek i `EventRepository`/controller, FK `Restrict` som sikkerhedsnet).
- **Ikke med:** Tildelinger og "besat"-tal (step 05), frigivelse (step 06). Reglerne om aktive tildelinger ved sletning og sænket kapacitet kommer i step 05.

## Plan
1. [ ] Model `Shift` + enum `ShiftStatus`, `ShiftConfiguration` (index på `EventId`+`StartUtc`, `CHECK` på `Capacity`), `DbSet` og migration `AddShiftsTable`.
2. [ ] `extensions/DanishTime.cs` (eller `DateTimeExtensions`): dansk dato + klokkeslæt → UTC og UTC → dansk tid, med tests for sommertid.
3. [ ] `repositories/Shifts/` med `Documents/Shifts.md`. Al læsning til brugere skal senere filtrere på `Status == Open`. Skriv det som faldgrube i dokumentet.
4. [ ] `ShiftsController` + `viewmodels/Shifts/` + `views/Shifts/` (Create, Edit). Validering: slut efter start, kapacitet 1–500, vagtens dato inden for begivenhedens periode.
5. [ ] Vagttabellen og statusfilteret på `Events/Details`. Fælles partial til statusbadges.
6. [ ] Blokér sletning af en begivenhed med vagter (besked: "Arkivér begivenheden i stedet").
7. [ ] Tests: en ny vagt er `Draft`, også når POST'en indeholder `Status=Open`. Kapacitet 0 og slut før start afvises. En bruger uden admin får "Ingen adgang". En begivenhed med vagter kan ikke slettes. Sommertidsomregning.
8. [ ] Opdatér `web/.claude/CLAUDE.md`: Repositories, Controllers og endpoints, Extensions.
9. [ ] Opdatér manualen: afsnittet **Vagter** (opret, ret, slet, status Kladde) med skærmbilleder (eller Manglende billeder), ændringslog og `.manuals/README.md`.
10. [ ] Opdatér Status for K06/K07/K10 i `.project/krav/krav.md`.
11. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 04 - Vagter og kladder (admin)" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/data/Models/Shift.cs`, `web/data/Models/ShiftStatus.cs`, `web/data/Configurations/ShiftConfiguration.cs`, `web/data/WebDbContext.cs`, `web/data/Migrations/` (ny/ændret)
- `web/extensions/` (ny)
- `web/repositories/Shifts/` (ny), `web/repositories/Events/` (ændret)
- `web/controllers/ShiftsController.cs`, `web/viewmodels/Shifts/`, `web/views/Shifts/` (nye)
- `web/controllers/EventsController.cs`, `web/viewmodels/Events/`, `web/views/Events/Details.cshtml` (ændret)

## Beslutninger
- Kun statusserne Kladde (`Draft`) og Åben (`Open`). PDF'en nævner kun de to. Enummet kan udvides senere (foreslået af Claude).
- Tidspunkter gemmes i UTC og omregnes eksplicit med `Europe/Copenhagen`, så visningen ikke afhænger af serverens tidszone (foreslået af Claude).
- En vagt skal ligge inden for begivenhedens periode (foreslået af Claude. Periode kan rettes på begivenheden).
- Forhåndstildeling sker på vagtens egen side (step 05), ikke i oprettelsesformularen som i mockup 04. Så kan formularen ikke åbne eller tildele ved en fejl (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt: opret tre vagter under "Skolefest" (fx Kiosk 17:00–19:00, Oprydning 21:00–22:30), se dem som **Skjult – kladde** i tabellen, ret én, slet én.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
