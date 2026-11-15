# Step 08 - Realtidsopdatering med SignalR

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 07 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Når admin åbner for tilmelding, eller en plads bliver taget eller ledig, opdateres åbne sider hos alle med det samme, uden at de skal genindlæse. Krav: K12, K17 (live), K10 (ingen lækage).

## Omfang
- **Med:**
  - `infrastructure/Realtime/ShiftsHub.cs` på `/hubs/shifts`. Kræver login (fallback-policyen), og klienter kan ikke sende noget til hubben.
  - `IShiftUpdatesNotifier` (i `infrastructure/Realtime/`) med `IHubContext<ShiftsHub>`. Kaldes af managers **efter** commit. Fejl logges og kastes ikke, da siderne også henter friske data ved genforbindelse.
  - Beskeder (kun id'er og tal for **åbne** vagter, aldrig titler på kladder og aldrig persondata):
    - `shiftsOpened` (`eventId`) til alle, når admin åbner.
    - `shiftsChanged` (`shiftIds`) til alle, når åbne vagter får tildelinger eller afmeldinger.
    - `myShiftsChanged` kun til den berørte bruger (`Clients.User`), når admin tildeler eller fjerner.
  - Klienten: `wwwroot/lib/microsoft-signalr/signalr.min.js` (fast version, ingen CDN) og `wwwroot/js/realtime.js`. På **Ledige vagter** henter den tabellen igen som partial (`Signup/Table`) og bevarer brugerens valg. På **Mine vagter** genindlæses listen. På `Events/Details` opdateres "Besat". Ved genforbindelse hentes friske data. En lille statuslinje viser "Forbindelsen er afbrudt – prøver igen".
  - `builder.Services.AddSignalR()` og `app.MapHub<ShiftsHub>("/hubs/shifts")` i `Program.cs`.
- **Ikke med:** Push til lukkede apps (step 10).

## Plan
1. [ ] Hub, notifier og registrering i `Program.cs` (hub-mapping efter `UseAuthorization`).
2. [ ] Kald notifieren fra `AssignmentManager`, `EnrollmentManager` og frigivelsen (frigivelsen flyttes til en manager, hvis den kalder mere end repositoriet).
3. [ ] `Signup/Table` (GET, partial view) og `realtime.js`. Hent SignalR-klienten én gang og læg den i `wwwroot/lib/` med versionen i filnavn/kommentar.
4. [ ] Tests med `Microsoft.AspNetCore.SignalR.Client` i `web.Tests`: en indlogget bruger modtager `shiftsOpened`, når admin åbner. Ændringer på kladder sender ingen beskeder. `myShiftsChanged` når kun den berørte bruger. En anonym forbindelse afvises.
5. [ ] Opdatér `web/.claude/CLAUDE.md`: Infrastruktur, Controllers og endpoints. Under "Bevidste afvigelser": SignalR-hubben er et mapped endpoint uden controller, og managers kalder `IShiftUpdatesNotifier` (begge med begrundelse).
6. [ ] Opdatér manualen: kort afsnit om live-opdatering og statuslinjen, fejlfinding (fx proxy uden websockets), ændringslog og `.manuals/README.md`.
7. [ ] Opdatér Status for K12/K17 i `.project/krav/krav.md`.
8. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 08 - Realtidsopdatering med SignalR" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/infrastructure/Realtime/` (ny)
- `web/managers/Assignments/`, `web/managers/Enrollment/` (ændret), evt. `web/managers/Publishing/` (ny)
- `web/controllers/SignupController.cs`, `web/views/Signup/` (ændret: partial)
- `web/wwwroot/lib/microsoft-signalr/`, `web/wwwroot/js/realtime.js` (nye)
- `web/views/Shared/_Layout.cshtml`, `web/Program.cs` (ændret)
- `web.Tests/` (nye tests)

## Beslutninger
- SignalR i stedet for polling, fordi PDF'en kræver, at vagterne bliver synlige "i samme sekund" (krav-pakken).
- Beskederne er "noget har ændret sig"-signaler, og siden henter selv data med sin egen adgangskontrol. Så kan en besked aldrig lække mere, end brugeren må se (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt med to browsere: B står på **Ledige vagter**, admin åbner "Skolefest" → vagterne dukker op hos B uden genindlæsning. C tager en plads → tallet falder hos B.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
