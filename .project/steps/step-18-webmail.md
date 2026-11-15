# Step 18 - Webmail: systemets postkasse

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 11, Step 13 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Systemet har sin egen postkasse. Alle mails sendes fra den (step 11), så når en forælder svarer på en invitation, bekræftelse eller påmindelse, lander svaret i systemets indbakke. Administratoren kan læse, besvare, skrive og rydde op i beskederne direkte i systemet i en lille webmail via IMAP og SMTP. Krav: K22.

## Omfang
- **Med:**
  - **`MailboxController`** (**Indbakke**, `AdminOnly`):
    - `Index`: mapperne Indbakke, Sendt og Papirkurv. Liste med afsender, emne og dato, ulæste fremhævet, søgning på afsender/emne og 50 pr. side.
    - `Read`: viser mailen og markerer den som læst. Tekstdelen vises først. HTML vises kun renset (ingen scripts, events eller formularer), og eksterne billeder blokeres. Vedhæftede filer listes og kan hentes.
    - `Reply`: svar med citat, `Re:` i emnet og `In-Reply-To`/`References`, så det bliver i samme tråd hos forælderen.
    - `Compose`: ny mail til en bruger valgt fra listen eller til en fri adresse.
    - `MarkUnread` og `Delete` (flyt til Papirkurv) som POST.
  - **Afsender genkendes:** er afsenderen en brugers e-mail, vises navnet med link til brugerens profil.
  - **`services/Mail/`** udvides med IMAP (MailKit `ImapClient`): hent liste, hent mail, markér læst/ulæst, flyt til papirkurv, hent vedhæftning, antal ulæste og gem kopi i Sendt (`APPEND`). IMAP-fejl oversættes til `MailException` efter `services.md` regel 15 (502/504 med konfigurationsnøglernes navne).
  - **Mapper:** Sendt og Papirkurv findes via IMAP special-use, med `Mail:Imap:SentFolder`/`Mail:Imap:TrashFolder` som fallback.
  - **Kopi i Sendt:** mails skrevet i webmailen gemmes i Sendt, styret af `Mail:Imap:SaveSentCopy` (nogle udbydere, fx Gmail, gemmer selv, og så slås den fra). Systemets automatiske mails (påmindelser m.m.) gemmes ikke i Sendt.
  - **Ulæste:** `MailboxPollWorker` henter antal ulæste hvert `Mail:Imap:PollIntervalSeconds` (standard 300) og gemmer det i en status-singleton. Det vises som KPI-kort "Ulæste beskeder" på dashboardet og øverst i **Indbakke**. Ved IMAP-fejl vises "Postkassen kunne ikke nås", og dashboardet virker stadig.
  - Mails gemmes ikke i `app.db`. Postkassen på mailserveren er den eneste kilde, så persondata ikke findes to steder.
- **Ikke med:** Vedhæftning af filer ved afsendelse, flere mapper, regler og autosvar, adgang for forældre.

## Plan
1. [ ] IMAP-metoderne i `services/Mail/` med `Dtos/Mailbox/` og opdateret `Documents/Mail.md`.
2. [ ] Konfiguration `Mail:Imap:SaveSentCopy`, `Mail:Imap:SentFolder`, `Mail:Imap:TrashFolder` og `Mail:Imap:PollIntervalSeconds` i `appsettings.json` og `.env.example` (`Mail__Imap__SaveSentCopy` osv.).
3. [ ] Rensning af HTML-mails med en HTML-sanitizer-pakke (fx `HtmlSanitizer`), uden eksterne billeder. Tekstdelen foretrækkes.
4. [ ] `managers/Mailbox/` (`MailboxManager`): kombinerer mail-servicen og `Users`/`Profiles` (genkendelse af afsender, modtagerliste ved ny mail).
5. [ ] `MailboxController` + `viewmodels/Mailbox/` + `views/Mailbox/` (Index, Read, Reply, Compose). Menupunktet **Indbakke** i admin-menuen.
6. [ ] `MailboxPollWorker` og status-singleton i `infrastructure/Notifications/`. KPI-kortet på dashboardet.
7. [ ] Tests med en falsk mail-service: kun admin har adgang (også POST). Svar får `Re:` og `In-Reply-To`/`References`. HTML med `<script>`, `onerror` og eksterne billeder renses. Afsender genkendes som bruger. IMAP-fejl giver en forståelig besked og vælter ikke dashboardet. POST uden anti-forgery-token → 400.
8. [ ] Manuel test mod en rigtig test-postkasse: lokalt med smtp4dev (har både SMTP og IMAP) eller systemets egen postkasse. Send en påmindelse, svar fra en almindelig mailklient, læs og besvar svaret i **Indbakke**.
9. [ ] Opdatér `web/.claude/CLAUDE.md`: Services, Managers, Workers, Infrastruktur, Controllers og endpoints, Konfiguration (pakken til HTML-rensning).
10. [ ] Opdatér manualen: **Indbakke** (læs, svar, skriv, slet), opsætning af systemets postkasse under **Konfiguration og drift**, fejlfinding (IMAP), skærmbilleder (eller Manglende billeder), ændringslog og `.manuals/README.md`.
11. [ ] Opdatér Status for K22 i `.project/krav/krav.md`.
12. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 18 - Webmail: systemets postkasse" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/services/Mail/` (ændret: IMAP), `web/managers/Mailbox/`, `web/bgworkers/MailboxPollWorker.cs` (nye)
- `web/controllers/MailboxController.cs`, `web/viewmodels/Mailbox/`, `web/views/Mailbox/` (nye)
- `web/infrastructure/Notifications/`, `web/managers/Dashboard/`, `web/views/Dashboard/`, `web/views/Shared/_Layout.cshtml` (ændret)
- `web/web.csproj` (pakke), `web/Program.cs`, `web/appsettings.json`, `.env.example` (ændret)

## Beslutninger
- Webmail til svar på systemets mails, med systemets egen postkasse via SMTP og IMAP (truffet af brugeren 2026-11-15).
- Kun administratorer har adgang til **Indbakke**, fordi mails fra forældre kan indeholde persondata (foreslået af Claude).
- Mails læses direkte fra IMAP og kopieres ikke til `app.db` (foreslået af Claude).
- Mailindhold vises som tekst eller renset HTML uden eksterne billeder, for at undgå XSS og sporingspixels (foreslået af Claude).
- Systemets automatiske mails gemmes ikke i Sendt, så mappen kun viser mails skrevet i webmailen (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt som beskrevet i plan-punkt 8.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Hvilken postkasse systemet får (adresse, SMTP- og IMAP-server). Brugeren beslutter. Indtil da testes lokalt med smtp4dev.
- Skal man kunne vedhæfte filer, når man skriver en mail? Ikke med nu. Kan tilføjes som et senere step.
