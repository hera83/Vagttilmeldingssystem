# Step 07 - Ledige vagter og samlet tilmelding

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 02, Step 06 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Forældrenes hovedfunktion: se de åbne vagter, vælge flere og tilmelde sig samlet efter først-til-mølle. Serveren afgør alt. Krav: K02, K13, K14, K15.

## Omfang
- **Med:**
  - `SignupController` (**Ledige vagter**, alle indloggede): `Index` viser kun vagter med `Status == Open`, grupperet med begivenhedskort øverst (antal ledige) og en tabel/kortliste efter mockup 05 (begivenhed, dato, tid, sted, ledige pladser). Filtre: begivenhed og dato. Vagter, brugeren allerede er på, vises som "Du er på". Fulde vagter vises som "Fuld" uden afkrydsning.
  - Multivalg: afkrydsning + panelet **Valgte vagter (n)** med **Bekræft tilmelding** (sticky på mobil). Formularen virker uden JS. JS (`wwwroot/js/signup.js`) opdaterer kun panelet.
  - POST `Signup/Enroll` med `ShiftIds` → `managers/Enrollment/` (`EnrollmentManager`): tjek komplet profil (K02) → `AssignmentRepository.CreateManyAsync(..., Source = Self, kunÅbne = true)`.
  - Udfald: **Succes** → redirect til **Mine vagter** med "Du er tilmeldt n vagter". **Konflikt** → siden vises igen med friske tal, de berørte vagter markeret og en besked som "Kiosk 17:00–19:00 blev fuld, før din tilmelding blev gennemført. Du er ikke tilmeldt nogen af vagterne. Vælg igen." **Profil mangler** → besked med link til **Min profil**, og knappen er deaktiveret (serveren afviser også).
  - En kladdevagt eller en andens skjulte vagt i POST'en afvises som `NotOpen`/`NotFound` uden at afsløre noget om vagten.
  - Admins forside (`Home/Index`) linker til **Begivenheder**. Brugernes forside viser næste vagt og et link til **Ledige vagter**.
- **Ikke med:** Live-opdatering af tal (step 08), bekræftelsesmail (step 11), offline-spærring (step 09).

## Plan
1. [ ] Læsemodel i `ShiftRepository` (eller ny metode) til åbne vagter med ledige pladser, filtreret på `Status == Open` i forespørgslen.
2. [ ] `EnrollmentManager` + `Dtos/EnrollResult` + `Documents/Enrollment.md`.
3. [ ] `SignupController` + `viewmodels/Signup/` + `views/Signup/Index.cshtml` (tabel på desktop, kort på mobil) + `wwwroot/js/signup.js`. Menupunktet **Ledige vagter**.
4. [ ] Konfliktvisning med `ModelState`, friske tal og bevaret valg minus konflikterne.
5. [ ] Integrationstests via `WebApplicationFactory`: 3 vagter → 3 tildelinger. Batch med én fuld vagt → 0 nye. Profil mangler → afvist. Kladde-id i POST → afvist og ikke vist. Uden anti-forgery-token → 400.
6. [ ] Samtidighedstests: 50 brugere om sidste plads → præcis 1. To parallelle multivalg med overlap → ingen overbooking, begge requests afsluttes (ingen timeout).
7. [ ] Opdatér `web/.claude/CLAUDE.md`: Managers, Controllers og endpoints.
8. [ ] Opdatér manualen: **Tilmeld dig vagter** (trin for forældre), konfliktbeskeden og skærmbilleder desktop/mobil (eller Manglende billeder), ændringslog og `.manuals/README.md`.
9. [ ] Opdatér Status for K02/K13/K14/K15 i `.project/krav/krav.md`.
10. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 07 - Ledige vagter og samlet tilmelding" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/repositories/Shifts/`, `web/repositories/Assignments/` (ændret)
- `web/managers/Enrollment/` (ny)
- `web/controllers/SignupController.cs`, `web/viewmodels/Signup/`, `web/views/Signup/`, `web/wwwroot/js/signup.js` (nye)
- `web/controllers/HomeController.cs`, `web/views/Home/Index.cshtml`, `web/views/Shared/_Layout.cshtml` (ændret)
- `web.Tests/` (nye tests)

## Beslutninger
- Ingen "du har fået pladsen", før transaktionen er gennemført. Siden opdateres først efter serverens svar (krav-pakken).
- Overlappende tidsrum blandt en brugers vagter blokeres ikke, fordi det ikke er et krav (foreslået af Claude, kan tilføjes).
- Admins kan også bruge **Ledige vagter** og tilmelde sig som en almindelig bruger (foreslået af Claude: PDF'en siger "tildele vagter til sig selv").

## Test
- `dotnet build` og `dotnet test`.
- Manuelt med to browsere: bruger B vælger tre vagter og bekræfter → de står under **Mine vagter**. Bruger C og B prøver samtidig om sidste plads → én får den, den anden får konfliktbeskeden uden delvis tilmelding.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
