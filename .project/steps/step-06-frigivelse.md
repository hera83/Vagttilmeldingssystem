# Step 06 - Frigivelse: Åbn for tilmelding

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 05 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Administratoren bestemmer, hvornår vagterne bliver synlige. Én bekræftet handling åbner kladderne i en begivenhed på én gang. Krav: K08, K10, K12 (uden realtid, som kommer i step 08).

## Omfang
- **Med:**
  - Knappen **Åbn for tilmelding** øverst på `Events/Details` (mockup 03), kun synlig, når begivenheden har kladder.
  - Bekræftelsessiden `Events/Open/{id}` (GET): "Er du sikker på, du vil åbne for tilmeldingen til perioden [dato] til [dato]?" med antal vagter og listen over kladder (alle valgt, kan fravælges). Perioden er første og sidste dato blandt de valgte vagter. En side frem for en JavaScript-dialog, så den virker med tastatur og uden JS.
  - POST `Events/Open/{id}` → `ShiftRepository.OpenAsync(eventId, shiftIds, openedBy)` i én transaktion: kun `Draft` → `Open`, sætter `OpenedAt`/`OpenedByUserId`. Idempotent: et gentaget POST åbner 0 vagter og viser "Der var ingen kladder at åbne".
  - Forhåndstildelte vagter bevarer deres tildelinger, og kun de resterende pladser bliver ledige.
  - Antal kladder og åbne vagter pr. begivenhed i `Events/Index`.
- **Ikke med:** Listen over ledige vagter (step 07), realtid (step 08), push ved frigivelse (step 10).

## Plan
1. [ ] `ShiftRepository.OpenAsync` + `Documents/Shifts.md`.
2. [ ] `Events/Open` (GET + POST) med `OpenEventsViewModel` og view. Tom eller ugyldig liste afvises (kun vagter fra den begivenhed og kun kladder).
3. [ ] Knappen på `Events/Details` og tællerne på `Events/Index`.
4. [ ] Tests: åbning ændrer kun de valgte kladder. Vagter fra en anden begivenhed i POST'en ignoreres/afvises. Gentaget POST ændrer intet. En bruger uden admin får "Ingen adgang". `OpenedAt` sættes.
5. [ ] Opdatér `web/.claude/CLAUDE.md`: Controllers og endpoints.
6. [ ] Opdatér manualen: **Åbn for tilmelding** med trin, bekræftelsessidens skærmbillede (eller Manglende billeder), ændringslog og `.manuals/README.md`.
7. [ ] Opdatér Status for K08 i `.project/krav/krav.md`.
8. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 06 - Frigivelse: Åbn for tilmelding" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/repositories/Shifts/` (ændret)
- `web/controllers/EventsController.cs`, `web/viewmodels/Events/`, `web/views/Events/` (ændret/nye views)

## Beslutninger
- Frigivelsen sker pr. begivenhed, med mulighed for at fravælge enkelte kladder (foreslået af Claude: mockup 03 viser frigivelse pr. begivenhed).
- En åben vagt kan ikke sættes tilbage til kladde, fordi forældre allerede kan have tilmeldt sig (foreslået af Claude).
- Ingen audit-tabel: `OpenedAt`/`OpenedByUserId` er historikken (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt: åbn "Skolefest" med én vagt fravalgt. Tjek badges (**Åben for tilmelding** / **Skjult – kladde**), og send formularen igen med tilbage-knappen og genindlæsning.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
