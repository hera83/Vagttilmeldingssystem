# Step 10 - Web push ved frigivelse

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 08, Step 09 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Forældre, der har sagt ja til push på deres enhed, får en besked på telefonen, når admin åbner for tilmelding, også når appen er lukket. Det lægger også køen og workeren, som e-mail (step 11) og påmindelser (step 12) bruger. Krav: K18, K10.

## Omfang
- **Med:**
  - **Service:** `services/Push/` (light pattern) med en web push-pakke til VAPID og kryptering. Pakken vælges i steppet: `Lib.Net.Http.WebPush` (vedligeholdt) eller `WebPush`. `PushException` + `case` i `GlobalExceptionHandler`. Udløbne abonnementer (404/410) meldes tilbage, så de kan slettes.
  - **Konfiguration:** `Push:PublicKey`, `Push:PrivateKey` (secret) og `Push:Subject` (`mailto:…`). Mangler nøglerne, er push slået fra med en tydelig besked på **Min profil** og en advarsel i loggen.
  - **Nøgler:** `dotnet run --project web -- --generate-vapid-keys` udskriver et nyt nøglepar og afslutter. Nøglerne logges aldrig.
  - **Tabel `PushSubscriptions`:** `UserId` (cascade), `Endpoint` (unik), `P256dh`, `Auth`, `CreatedAt`, `LastSuccessAt?`. `repositories/PushSubscriptions/`.
  - **`PushController`** (indlogget): `Subscribe` og `Unsubscribe` (POST, JSON fra `fetch` med anti-forgery-header). Kun egne abonnementer.
  - **Min profil, højre panel** (mockup 07): "Push-beskeder på denne enhed" med status Aktiv / Ikke slået til / Blokeret i browseren / Ikke understøttet ("På iPhone: føj appen til hjemmeskærmen først"). Knappen **Slå push til** beder om tilladelse, først når brugeren klikker.
  - **Service worker:** `push` → `showNotification`, `notificationclick` → åbner linket (fx `/Signup`).
  - **Kø `Notifications`:** `UserId`, `Channel` (`Push`/`Email`), `Kind`, `Title`, `Body`, `Url`, `DedupKey` (unik), `Status` (`Pending`/`Sent`/`Failed`/`Skipped`), `Attempts`, `NextAttemptAt`, `SentAt?`, `LastError?`. `repositories/Notifications/`.
  - **`NotificationWorker`** (`bgworkers/`): vækkes af et signal (`infrastructure/Notifications/NotificationSignal`) eller hvert 30. sekund. Finder først nyåbnede vagter (`Status == Open` og `OpenNotifiedAt == null`), opretter én generisk besked pr. bruger ("Nye vagter er åbne: Skolefest – 6 vagter") og markerer vagterne. Sender derefter ventende beskeder med begrænsede genforsøg. Idempotent via `DedupKey`, så en genstart aldrig sender dobbelt.
  - Ny kolonne `Shifts.OpenNotifiedAt` (migration).
- **Ikke med:** E-mail-kanalen (step 11), påmindelser (step 12).

## Plan
1. [ ] Vælg og tilføj pakken (version afprøves med `dotnet build`). `services/Push/` med `Documents/Push.md`, `Dtos/`, `Interfaces/`, `PushException` og `case` i `GlobalExceptionHandler`.
2. [ ] Konfiguration i `appsettings.json` (tomme værdier), `appsettings.Development.json` (lokale nøgler) og `.env.example` (`Push__PublicKey`, `Push__PrivateKey`, `Push__Subject`). Flaget `--generate-vapid-keys` i `Program.cs`.
3. [ ] Tabeller `PushSubscriptions` og `Notifications`, kolonnen `OpenNotifiedAt` og migration `AddPushAndNotifications`. Repositories med `Documents/`.
4. [ ] `PushController` + panelet på **Min profil** + `wwwroot/js/push.js` + `push`/`notificationclick` i service workeren.
5. [ ] `NotificationSignal`, `NotificationWorker` (scope pr. gennemløb, try/catch pr. gennemløb, se `bgworkers.md`) og en manager til "nyåbnede vagter → beskeder", hvis den kombinerer flere repositories. Frigivelsen sender signalet efter commit.
6. [ ] Tests med en falsk push-service: frigivelse giver én besked pr. bruger med abonnement. Et nyt gennemløb sender intet. Kladder giver aldrig beskeder. Beskeden indeholder ingen persondata. Udløbet abonnement slettes. `Subscribe` uden login → login. Uden anti-forgery-token → 400.
7. [ ] Opdatér `web/.claude/CLAUDE.md`: Databaser, Repositories, Managers, Services, Workers, Controllers og endpoints, Konfiguration (secret: `Push:PrivateKey`). Under "Bevidste afvigelser": `PushController` svarer `fetch`-kald med `NoContent()` i stedet for et view.
8. [ ] Opdatér manualen: **Push-beskeder** (slå til/fra, iPhone-kravet, at levering ikke kan garanteres), VAPID-nøgler under **Konfiguration og drift**, fejlfinding, ændringslog og `.manuals/README.md`.
9. [ ] Opdatér Status for K18/K10 i `.project/krav/krav.md`.
10. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 10 - Web push ved frigivelse" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/web.csproj` (pakke), `web/Program.cs`, `web/appsettings*.json`, `.env.example` (ændret)
- `web/services/Push/`, `web/repositories/PushSubscriptions/`, `web/repositories/Notifications/`, `web/infrastructure/Notifications/`, `web/bgworkers/NotificationWorker.cs` (nye)
- `web/data/Models/`, `web/data/Configurations/`, `web/data/Migrations/` (ny/ændret)
- `web/controllers/PushController.cs`, `web/wwwroot/js/push.js` (nye), `web/wwwroot/service-worker.js`, `web/views/Profile/Index.cshtml`, `web/infrastructure/ErrorHandling/GlobalExceptionHandler.cs` (ændret)

## Beslutninger
- Alle udgående beskeder (push nu, e-mail og påmindelser senere) går gennem én kø med `DedupKey` og én worker. Det giver genforsøg, idempotens og status til dashboardet ét sted (foreslået af Claude, i stedet for ChatGPT-pakkens outbox).
- "Nyåbnede vagter" findes af workeren ud fra `OpenNotifiedAt` i stedet for at blive skrevet i samme transaktion som frigivelsen. Så skal der ikke laves en transaktion på tværs af repositories, og intet går tabt ved et nedbrud (foreslået af Claude).
- Abonnementet på enheden er samtykket til push. Der er ingen ekstra profilindstilling for push (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt i Edge/Chrome på desktop: **Slå push til** → tillad → admin åbner en begivenhed → notifikationen vises og åbner **Ledige vagter**. Blokér tilladelsen og se statusteksten.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Push på iPhone og Android kræver https på en server. Planen forbereder kun Docker (step 16), så det testes i step 17, hvis appen kører med https et sted. Ellers står det som ikke testet.
