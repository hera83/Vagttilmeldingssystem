# Step 11 - E-mail: invitation, glemt adgangskode og bekræftelser

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 07, Step 10 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Systemet kan sende e-mails. Admin kan invitere forældre i stedet for at udlevere adgangskoder, forældre kan selv nulstille en glemt adgangskode og får en bekræftelse, når de tilmelder sig. E-mail er også den anden kanal til påmindelser (step 12). Krav: K02/K03 (onboarding), grundlag for K19/K20.

## Omfang
- **Med:**
  - **Service:** `services/Mail/` (light pattern uden HTTP) med MailKit. `MailException` + `case` i `GlobalExceptionHandler`. Fejl oversættes efter `services.md` regel 15 (forbindelse/TLS, login, timeout).
  - **Konfiguration** i sektionen `Mail` med én undersektion pr. forbindelse (`configuration.md` regel 5), alle i `appsettings.json` med tomme værdier og i `.env.example` i `__`-notation:
    - SMTP (afsendelse): `Mail:Smtp:Host`, `Mail:Smtp:Port`, `Mail:Smtp:Security` (`StartTls`/`SslOnConnect`/`None`), `Mail:Smtp:UserName`, `Mail:Smtp:Password` (secret).
    - IMAP: `Mail:Imap:Host`, `Mail:Imap:Port`, `Mail:Imap:Security`, `Mail:Imap:UserName`, `Mail:Imap:Password` (secret). IMAP bruges af webmailen i step 18. I dette step lægges kun konfigurationen.
    - Fælles: `Mail:FromAddress`, `Mail:FromName`, `Mail:PickupDirectory` (kun til udvikling: mails gemmes som `.eml`-filer i stedet for at blive sendt).
    - `App:BaseUrl` (til links i mails).
  - **Systemets egen postkasse:** alle mails sendes fra `Mail:FromAddress`, som er systemets egen postkasse, så forældres svar lander i den indbakke, webmailen viser (step 18).
  - **E-mail-kanalen** i `NotificationWorker`: sender `Channel = Email` til brugerens e-mail (brugernavnet). Brødtekst med links ryddes, når mailen er sendt.
  - **Tabel `PasswordTokens`:** `UserId` (cascade), `TokenHash` (SHA-256), `Purpose` (`Invitation`/`Reset`), `ExpiresAt`, `UsedAt?`. Kun hashet gemmes. Et nyt token annullerer tidligere tokens med samme formål. `repositories/PasswordTokens/`.
  - **Invitation:** **Brugere → Opret** får afkrydsningen **Send invitation på e-mail** (standard: til). Er den valgt, skriver admin ingen adgangskode. Linket `/Account/SetPassword?token=…` gælder 7 dage. **Send invitation igen** på brugerlisten.
  - **Glemt adgangskode:** link på login-siden → `/Account/ForgotPassword` (anonym) svarer altid "Hvis e-mailen findes, har vi sendt et link". Linket gælder 1 time. `/Account/ResetPassword` (anonym) sætter ny adgangskode (mindst 12 tegn) via `SetPasswordAsync`, så andre sessioner logges ud.
  - **Bekræftelse:** efter en vellykket tilmelding sendes en mail med vagterne (dato og tid i dansk tid).
  - **Min profil:** "E-mail-beskeder" til/fra (ny kolonne `Profiles.EmailNotifications`, standard til). Den styrer frigivelsesbeskeder, bekræftelser og påmindelser. Invitation og nulstilling sendes altid.
  - E-mail ved frigivelse for brugere med e-mail-beskeder slået til (samme gennemløb som push i step 10).
- **Ikke med:** Påmindelser (step 12), webmail og IMAP-funktioner (step 18), rate limiting på glemt adgangskode (step 15).

## Plan
1. [ ] Tilføj MailKit. `services/Mail/` med `Documents/Mail.md`, `Interfaces/`, `Dtos/`, `MailException` og `case` i `GlobalExceptionHandler`.
2. [ ] Konfiguration: alle `Mail:Smtp:*`, `Mail:Imap:*`, `Mail:*` og `App:BaseUrl` i `appsettings.json` (tomme værdier), `appsettings.Development.json` (lokale værdier, `Mail:PickupDirectory` til `app_files/MailPickup/`) og `.env.example` (`Mail__Smtp__Host`, `Mail__Smtp__Port`, `Mail__Smtp__Security`, `Mail__Smtp__UserName`, `Mail__Smtp__Password`, `Mail__Imap__Host`, `Mail__Imap__Port`, `Mail__Imap__Security`, `Mail__Imap__UserName`, `Mail__Imap__Password`, `Mail__FromAddress`, `Mail__FromName`, `App__BaseUrl`). Manglende værdier giver en `MailException` med nøglens navn (`services.md` regel 15).
3. [ ] IMAP: kun konfigurationen i dette step. Skriv i `Documents/Mail.md`, at IMAP-metoderne kommer i step 18 (webmail).
4. [ ] Tabellen `PasswordTokens`, kolonnen `Profiles.EmailNotifications` og migration. `repositories/PasswordTokens/` med `Documents/`.
5. [ ] `managers/AccountRecovery/` (Users + PasswordTokens + Notifications): invitation, glemt adgangskode og nulstilling. Forventede udfald returneres som enum (fx `TokenInvalid`, `TokenExpired`).
6. [ ] `AccountController`: `ForgotPassword`, `SetPassword`, `ResetPassword` (GET + POST, anonyme) med ViewModels og views. `UsersController`: invitation ved oprettelse og **Send invitation igen**.
7. [ ] E-mail-kanalen i `NotificationWorker`. Bekræftelsesmailen fra `EnrollmentManager` efter commit. E-mail ved frigivelse.
8. [ ] Danske mailtekster (emne og brødtekst, både tekst og simpel HTML), uden andre persondata end modtagerens egne vagter.
9. [ ] Tests med en falsk mail-service: invitation → mail med link → adgangskoden sættes → login virker. Token kan kun bruges én gang og udløber. Glemt adgangskode for en ukendt e-mail giver samme svar og ingen mail. Gamle sessioner logges ud efter nulstilling. Bekræftelse kun ved succes. `EmailNotifications = false` → ingen frigivelses-/bekræftelsesmail.
10. [ ] Opdatér `web/.claude/CLAUDE.md`: Databaser, Repositories, Managers, Services, Controllers og endpoints (anonyme actions med begrundelse), Konfiguration (secrets: `Mail:Smtp:Password`, `Mail:Imap:Password`). Under "Bevidste afvigelser": oprettelse af brugere uden adgangskode, når der sendes invitation.
11. [ ] Opdatér manualen: **Invitér en forælder**, **Glemt adgangskode**, **E-mail-beskeder**, mailopsætning under **Konfiguration og drift**, fejlfinding (SMTP), skærmbilleder (eller Manglende billeder), ændringslog og `.manuals/README.md`.
12. [ ] Opdatér `.gitignore`, hvis `app_files/MailPickup/` kræver det (kun `.gitkeep` må med).
13. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 11 - E-mail: invitation, glemt adgangskode og bekræftelser" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/web.csproj`, `web/Program.cs`, `web/appsettings*.json`, `.env.example` (ændret)
- `web/services/Mail/`, `web/repositories/PasswordTokens/`, `web/managers/AccountRecovery/` (nye)
- `web/bgworkers/NotificationWorker.cs`, `web/managers/Enrollment/`, `web/repositories/Profiles/` (ændret)
- `web/controllers/AccountController.cs`, `web/controllers/UsersController.cs`, `web/viewmodels/Account/`, `web/viewmodels/Users/`, `web/views/Account/`, `web/views/Users/`, `web/views/Profile/Index.cshtml` (ændret/nye)
- `web/data/` (model, configuration, migration)

## Beslutninger
- Push + e-mail (truffet af brugeren).
- Invitationslink frem for at admin udleverer adgangskoder, så admin aldrig kender forældrenes adgangskoder (foreslået af Claude).
- Mails med token-link sendes også via køen, og brødteksten ryddes efter afsendelse. Så kan en SMTP-fejl ikke vælte en side, og der ligger ikke gyldige links i databasen længere end nødvendigt (foreslået af Claude).
- `Mail:PickupDirectory` til udvikling, så alt kan testes uden en rigtig SMTP-server (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt med `Mail:PickupDirectory`: opret en bruger med invitation → åbn `.eml`-filen → følg linket → sæt adgangskode → log ind. Glemt adgangskode-flowet. Tilmeld vagter → bekræftelsesmail.
- Med en rigtig SMTP-konto (når brugeren har én): én testmail til egen adresse.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Hvilken postkasse systemet får (adresse og SMTP/IMAP-server, fx Office 365, Simply eller Gmail med app-adgangskode). Brugeren beslutter. Indtil da testes med `Mail:PickupDirectory` eller smtp4dev lokalt.
