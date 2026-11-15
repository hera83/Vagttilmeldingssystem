# Step 12 - Automatiske påmindelser 48 og 24 timer før

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 10, Step 11 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Et baggrundsjob scanner databasen én gang i timen og sender påmindelser 48 og 24 timer før vagtens start via push og e-mail. Det sker højst én gang pr. påmindelse, også ved genstart. Krav: K19, K20, K21.

## Omfang
- **Med:**
  - `bgworkers/ReminderWorker.cs`: kører ved opstart og derefter hvert `Reminders:WorkerIntervalSeconds` (standard 3600 = én gang i timen, jf. PDF'en). Kan slås fra med `Reminders:Enabled`.
  - `managers/Reminders/` (`ReminderManager`): finder aktive tildelinger, hvor tidspunktet er nået, og lægger beskeder i køen (`Notifications`) med `DedupKey = reminder:{48|24}:{assignmentId}:{StartUtc}:{kanal}`. Det unikke index gør gentagne og parallelle scanninger ufarlige. Til sidst vækkes `NotificationWorker`.
  - Regler:
    - 48-timers: når `nu ≥ start − 48 t` og `nu < start − 24 t`.
    - 24-timers: når `nu ≥ start − 24 t` og `nu < start`.
    - En tildeling, der oprettes mindre end 24 timer før start, får kun 24-timers påmindelsen. En vagt, der er startet, får ingen.
    - Afmeldte tildelinger får ingen påmindelse. Er beskeden allerede i køen, springes den over ved afsendelse (`Skipped`).
    - Flyttes vagtens starttid, giver den nye `StartUtc` nye påmindelser.
    - Påmindelser sendes for alle aktive tildelinger, også tildelte kladdevagter, fordi brugeren kan se dem i sin kalender.
  - Tekster som i PDF'en: "Du har en vagt om 2 dage: [dato] kl. [tid]" og "Husk din vagt i morgen kl. [tid]" + vagtens navn og sted, dansk tid.
  - Kanaler: push til brugerens abonnementer og e-mail, hvis `EmailNotifications` er slået til.
  - `TimeProvider` (registreret som `TimeProvider.System`) bruges til "nu", så testene kan styre uret.
  - `ReminderWorkerStatus` (singleton i `infrastructure/Notifications/`): seneste og næste scanning, antal oprettede beskeder og seneste fejl. Vises på dashboardet i step 13.
- **Ikke med:** Dashboard-visningen (step 13).

## Plan
1. [ ] `ReminderManager` + `Documents/Reminders.md` med reglerne ovenfor.
2. [ ] `ReminderWorker` efter skabelonen i `bgworkers.md` (`PeriodicTimer`, scope pr. gennemløb, try/catch). `ReminderWorkerStatus`.
3. [ ] `TimeProvider` i `Program.cs`, og brug den i `ReminderManager` og `NotificationWorker`.
4. [ ] Konfiguration `Reminders:WorkerIntervalSeconds` og `Reminders:Enabled` i `appsettings.json` og `.env.example` (`Reminders__WorkerIntervalSeconds`, `Reminders__Enabled`).
5. [ ] Tests med `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing` i `web.Tests`): 48 t og 24 t giver hver sin besked. To scanninger og to parallelle scanninger giver ingen dubletter. Tildeling 30 t før → kun 24 t. Afmeldt → ingen. Flyttet vagt → nye påmindelser. Sommertid i Danmark (fx vagt mandag efter sidste søndag i marts og oktober) beregnes i UTC, og teksten viser dansk tid. Ingen abonnementer og ingen e-mail → ingen besked.
6. [ ] Opdatér `web/.claude/CLAUDE.md`: Managers, Workers, Infrastruktur, Konfiguration.
7. [ ] Opdatér manualen: **Påmindelser** for forældre og drift (interval, op til ét interval forsinkelse, push er "best effort"), fejlfinding, ændringslog og `.manuals/README.md`.
8. [ ] Opdatér Status for K19/K20/K21 i `.project/krav/krav.md`.
9. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 12 - Automatiske påmindelser 48 og 24 timer før" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/managers/Reminders/`, `web/bgworkers/ReminderWorker.cs`, `web/infrastructure/Notifications/ReminderWorkerStatus.cs` (nye)
- `web/bgworkers/NotificationWorker.cs`, `web/Program.cs`, `web/appsettings.json`, `.env.example` (ændret)
- `web.Tests/` (nye tests, testpakke)

## Beslutninger
- Scanning én gang i timen som i PDF'en. "Præcis 48/24 timer" kan derfor betyde op til 60 minutter senere. Det står i manualen og i `krav.md` (afklaring 2) (krav-pakken).
- Idempotens via køens unikke `DedupKey` i stedet for en separat `ReminderDelivery`-tabel (foreslået af Claude: samme mekanisme som step 10).
- Starttiden indgår i nøglen, så en flyttet vagt får nye påmindelser (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt: sæt `Reminders:WorkerIntervalSeconds` til 60 i Development, opret en vagt, der starter om 47 timer, med en tildeling, og se push og `.eml`-fil inden for et minut. Genstart appen og tjek, at der ikke kommer en ny.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
