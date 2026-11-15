# Claude Code – bindende projektinstrukser for Vagttilmelding

Du skal bygge **det færdige fungerende system** specificeret i projektbeskrivelsen. Dette er et skole-/klassebaseret vagttilmeldingssystem, som leveres som en **PWA** på web; der skal **ikke** bygges to separate native apps. Du må ikke nøjes med mock UI, hardcodede eksempler eller en API uden integreret UI.

## Læs før arbejde

1. `README_START_HER.md`
2. `KRAV_OG_ACCEPTKRITERIER.md`
3. `TEKNISK_DESIGN.md`
4. `DESIGN_GUIDE.md`
5. Det nummererede `STEP_XX_*.md`, du er blevet bedt om.
6. Ved konflikt: `Projektbeskrivelse_Original.pdf` > `KRAV_OG_ACCEPTKRITERIER.md` > trininstruktioner > billeder.

## Arbejdsform

- **Ét STEP ad gangen**. Læs eksisterende kode/dokumentation først, lav en kort konkret plan, implementér, test, dokumentér og afslut med rapport. STOP og afvent brugerens GO.
- Genbrug eksisterende kode og conventions; undgå rewrites og unødvendige abstraheringer. Skriv rigtig fungerende kode, migrations, tests og relevant dokumentation.
- Opdatér `docs/STEP_STATUS.md`, `docs/ARCHITECTURE.md`, `docs/OPERATIONS.md`, `docs/API.md`, `docs/TESTS.md` når ændringerne kræver det. Notér faktiske kommandoer og resultater; påstå ikke, at tests er kørt, hvis de ikke er det.
- Når et trin kræver en hemmelighed, brug environment variables/user-secrets, `.env.example` med placeholders; aldrig hemmeligheder i repo, logs eller dokumenter.
- Ingen default produktions-admin eller kendte loginoplysninger. Admin oprettes via kontrolleret seed/CLI/invitation i sikker proces.
- **Denne instruktionspakke må ikke committes:** `.gitignore` skal indeholde `/CLAUDE.md`, `/.claude/`, `/STEP_*.md`, `/PROMPTS_TIL_CLAUDE_CODE.md` og eventuelle lokale Claude-instruktioner. Respektér andre eksisterende ignore-regler. `README_START_HER.md`, PDF og billeder kan blive uden for appens Git-repo, hvis det ønskes.
- Ingen push, hostingændring, produktionsdata-sletning eller live deployment uden eksplicit accept.

## Ufravigelige forretningsregler

- Kun rollerne **Administrator** og **Bruger**. Default registrering = Bruger.
- Nye vagter er **skjulte/kladder**. De må ikke optræde i ledige-vagter API, websider, realtids-events eller notifikationer før frigivelse.
- Administrator kan **forhåndstildele** skjulte vagter; kun den tildelte bruger ser sin egen vagt før frigivelse.
- Frigivelse skal være atomisk og kræve bekræftelse; efterfølgende ledige pladser styres **først-til-mølle** på server/database – aldrig med front-end-check alene.
- Tilmelding af flere vagter i én handling er **alt-eller-intet**; ved konflikt må brugeren ikke ende med delvist bookede vagter uden klar eksplicit ny handling.
- En tildeling er **låst**. Bruger kan ikke afmelde, bytte eller overdrage; **kun administrator** kan fjerne/åbne pladsen igen.
- 48-/24-timers påmindelser skal være idempotente. Webbrowsere kan ikke garantere tidspunktet for push-levering.
- Personoplysninger (herunder navn, adresse, telefon, e-mail, barn/klasse, trøjestørrelse, væske- og tivolibilletter) skal behandles sikkert; **tivolibilletter: højst 2 ark**.

## Design og tilgængelighed

- Brug `mockups/*.png` som visuel retning: klassisk enterprise, lys baggrund, blå primærfarve, overskuelige tabeller, responsive mobilkort og tydelig status.
- PWA skal fungere i mobilbrowser og kunne installeres, hvor platformen tillader det. Respekter brugerens notifikationstilladelse.
- Lav ikke UI-kopier med fiktive data i produktionsvisninger. Brug kun testdata i eksplicit udviklingsmiljø.
- Dansk brugerflade, tydelige fejl/feedback, loading, offline, tomme lister, tastaturbetjening og skærmlæseretiketter.

## Kvalitet og sikkerhed

- Brug sikker autentifikation og rettigheder på **alle** relevante API-endpoints, også SignalR/grupper.
- Sikr alle state-changing endpoints mod CSRF ved cookie-auth; beskyt mod IDOR, masseopdatering, XSS, SQL injection og rate abuse.
- SQL Server-transaktioner, constraints og kørselskonkurrencetests er obligatoriske for pladser.
- EF Core migrationer, integrationstest, E2E-tests (Playwright), lint/build og nødvendig logging er del af leverancen.

## Rapporteringsformat efter hvert trin

Skriv i din afslutning:

1. **Færdigt:** faktisk implementeret, fil-/moduloversigt.
2. **Test:** konkrete kommandoer og status (PASS/FAIL/IKKE KØRT).
3. **Kravdækning:** krav-ID'er, der nu er gennemført.
4. **Risici/blokeringer:** klart og kort.
5. **Næste trin:** hvilken STEP der nu kan begyndes.

Aldrig markér alle steps færdige på baggrund af enkeltstående mocks eller tests.
