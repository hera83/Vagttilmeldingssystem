# Teknisk baseline og arkitekturbeslutninger

Denne fil beskriver et **foreslået implementeringsdesign**, ikke noget der står i original-PDF'en. Brug én relativt enkel modulær monolit: nemmere at udvikle, teste og hoste i et homelab end mikroservices.

## Valgt stack

| Del | Teknologi | Hvorfor |
|---|---|---|
| API | ASP.NET Core / .NET 10 | Langtidsholdbar C#-backend med stærk typekontrol |
| Frontend | React + TypeScript + Vite | Responsiv, PWA, moderne enterprise UI |
| Data | SQL Server + EF Core migrations | Transaktionssikker kapacitet og kendt drift |
| Login | ASP.NET Core Identity + sikker HttpOnly-cookie + CSRF | Browser/PWA på samme oprindelse |
| Realtime | SignalR | Push-lignende live UI-opdatering efter frigivelse/bookinger |
| Browser-notifikationer | Service worker + VAPID Web Push | PWA på understøttede platforme |
| Baggrund | Hosted service / worker + SQL outbox | Idempotent og fejltolerant udsendelse |
| Tests | xUnit + WebApplicationFactory + test-SQL Server + Playwright | Rigtige integration-/konkurrencetests |

## Mappestruktur (Claude tilpasser eksisterende struktur uden unødvendig ombygning)

```text
src/
  Vagttilmelding.Api/             # Auth, API, SignalR og worker
  Vagttilmelding.Application/     # Domæneregler, commands, validators, interfaces
  Vagttilmelding.Infrastructure/  # EF Core, SQL Server, Web Push, mail, outbox
  Vagttilmelding.Web/             # React / TypeScript / Vite PWA

tests/
  Vagttilmelding.UnitTests/
  Vagttilmelding.IntegrationTests/
  Vagttilmelding.E2E/

docs/
  ARCHITECTURE.md
  API.md
  OPERATIONS.md
  TESTS.md
  STEP_STATUS.md
  decisions/
```

## Domæne og invariants

- `ApplicationUser` (Identity), `UserProfile`, eventuelt `Child` (enkelt navn/klasse er nok), `Event`, `Shift`, `Assignment`, `PushSubscription`, `NotificationOutbox`, `NotificationAttempt`, `ReminderDelivery`, `AuditEntry`.
- `Event` har dato/periode, navn, beskrivelse, lokation og frivillige vagtposter. En POC behøver ikke multi-tenant.
- `Shift` har tidsinterval (UTC), lokalt visningstidspunkt/`Europe/Copenhagen`, makskapacitet og `Draft/Open/Closed` ("Skjult" = Draft i brugerfladen). `Assignment` er aktivt medlem, kilde `Admin/Self`, created/cancelled-registrering.
- `Assignment` skal have unik aktiv `(ShiftId, UserId)`-constraint; DB-transaktion og lås sikrer, at **antal aktive <= kapacitet**, også på tværs af API-instanser.
- Tilmelding af flere vagter skal bruge deterministisk låserækkefølge på `ShiftId`, kapacitetskontrol og **én DB-transaktion**. Undlad at bruge kun `lock` i C# – det virker ikke på tværs af instanser.
- Publicering: DB-transaktion for alle valgte kladdevagter i begivenhed/periode, en AuditEntry og én outbox-event; først efter commit sker SignalR/Web Push.
- Afmeldingsmulighed findes **kun for administrator**. Låsning er permissions- og serverdomæneregel.
- Backend returnerer kun ejerens egne skjulte tildelinger; frontend bestemmer ikke synlighed alene.

## API-forslag (kan justeres, men dokumentér actual endpoints)

```text
POST /api/auth/register                # bruger + profil, validering
POST /api/auth/login
POST /api/auth/logout
GET  /api/me
PUT  /api/me/profile
GET  /api/me/shifts                   # åbne egne + skjulte egne tildelinger
GET  /api/events                      # kun offentligt tilgængelige events/vagter
GET  /api/shifts/available            # kun Open, med serverbaseret kapacitet
POST /api/enrollments/batch           # atomisk, auth + komplet profil
GET  /api/admin/events
POST /api/admin/events
PUT  /api/admin/events/{id}
GET  /api/admin/shifts
POST /api/admin/shifts                # default Draft
PUT  /api/admin/shifts/{id}
POST /api/admin/shifts/{id}/assign    # skjult/åben preassign
POST /api/admin/events/{id}/publish   # bekræftet scope + snapshot version
DELETE /api/admin/assignments/{id}    # admin-only logisk annullering
POST /api/me/push-subscriptions
DELETE /api/me/push-subscriptions/{id}
GET  /hubs/shifts                     # SignalR, autoriseret public/private events
```

## Sikkerhed og privatliv

- Cookie auth via same-origin HTTPS med `HttpOnly`, `Secure`, `SameSite`, CSRF-beskyttelse for alle ændringer. Identity-låse/rate limiting, password hashing, admin authorization policies.
- Administration må se nødvendige kontaktdata, forældre må kun se deres egne; audit for adminindgreb. Ingen CPR, betalingskort eller ekstra børneoplysninger.
- Klientcache **må ikke** cache personlige API-responser eller kritiske bookingsvar; offline viser blot senest viste usensitive shell og klar "kræver internet".
- Konfigurerbar log-redaktion og retention; klar privatlivstekst, ingen unødigt persondata i push.

## Drift

- Lokal SQL Server kan være container eller eksisterende instans; der må ikke være hardcodede credentials.
- Frontend bygges statisk og serveres fra API/same-origin på HTTPS i drift.
- IIS og Ubuntu/nginx er to dokumenterede mål; ét mål skal gennemprøves i den valgte driftsopsætning, begge får opsætningsanvisninger.
- Tests skal køre mod reel SQL Server-container/instans for at validere låsning. InMemory/SQLite tæller ikke som kapacitets-konkurrencetest.
