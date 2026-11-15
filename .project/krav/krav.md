# Krav: Vagttilmeldingssystem

Kravgrundlaget for opgaven **Vagttilmeldingssystem**. Alle steps i `.project/steps/` henviser hertil med krav-ID'erne K01–K22. Når et step er færdigt, opdateres kolonnen **Status** for de krav, steppet dækker.

| | |
|---|---|
| Kravkilde | [Projektbeskrivelse_Original.pdf](Projektbeskrivelse_Original.pdf) (3 sider) |
| Mockups | [mockups/](mockups/): 7 billeder, kun som visuel retning |
| Oprindelse | Uddrag af ChatGPT-pakken i `.poc/`, omsat til løsningens instrukser 2026-11-15 |
| Rangorden ved konflikt | Brugerens beslutninger (nedenfor) > PDF'en > denne fil > mockups |

## Brugerens beslutninger (2026-11-15)

| Emne | Beslutning |
|---|---|
| Platform | Én webapp, der kan installeres som PWA på iOS og Android. Ingen native apps. |
| Projekttype | **Web**: ét projekt `web` med Razor-views og cookie-login. Intet API med `x-api-key` og ingen Swagger. |
| Brugeroprettelse | **Kun administratoren opretter brugere.** Forælderen logger ind og udfylder sin profil, før vedkommende kan tilmelde sig. |
| E-mail | **Push og e-mail.** E-mail bruges til invitation, glemt adgangskode, bekræftelse af tilmelding og påmindelser. SMTP- og IMAP-konfigurationen (`Mail:Smtp:*`, `Mail:Imap:*`) står i `appsettings.json` og `.env.example`. |
| Systemets postkasse og webmail | Systemet har sin egen postkasse, og alle mails sendes fra den. Svar fra forældre hentes via IMAP, og administratoren kan læse, besvare, skrive og slette beskeder i en lille webmail i systemet (K22, step 18). |
| Drift | Projektet **forberedes til Docker** efter `.claude/docker.md` (Dockerfile, compose, `.dockerignore`, `.env.example`). Containeren bygges og køres ikke i planen. |
| `.env.example` | Oprettes i step 01 og opdateres i hvert step, der tilføjer konfiguration. |
| Git | Når et step er færdigt, committes og pushes ændringerne automatisk. |

## Omsætning fra ChatGPT-pakken

POC-pakken foreslog en anden teknisk stak. Løsningens instrukser i `.claude/` gælder:

| POC-pakken | Her | Hvorfor |
|---|---|---|
| React + Vite SPA | ASP.NET Core MVC med Razor-views og lidt vanilla JS i `wwwroot/js/` | `project.md`: alt bygges med controllere og views |
| SQL Server | SQLite (`app_dbs/app.db`) | `databases.md` regel 2. SQLite har én skriver ad gangen, og skrive-transaktioner startes med `BEGIN IMMEDIATE`, så "tæl pladser + indsæt" ikke kan overbooke. Testes mod en rigtig SQLite-fil. |
| ASP.NET Identity | Standardens cookie-login (`User`-tabel, `PasswordHasher<User>`) | `cookieauth.md` |
| `src/`- og `tests/`-projekter | `web/` og `web.Tests/` | `project.md`, `commands.md` |
| `docs/` (STEP_STATUS, ARCHITECTURE, API …) | `.project/` (planer og historik) og `.manuals/web/manual.md` | `planning.md`, `manuals.md` |
| Audit-tabel | Ingen. Historikken ligger i tidsstempler og i afmeldte tildelinger, som aldrig slettes. | Foreslået af Claude: færre tabeller, og dashboardets aktivitet kan bygges af dem. |
| Playwright E2E | xUnit-integrationstest med `WebApplicationFactory` + manuel browsertest | Foreslået af Claude: ingen Node-afhængighed. Kan tilføjes senere. |
| IIS + Ubuntu | Docker | Brugerens valg |

## Krav

| ID | Krav | Skal kunne vises | Steps | Status |
|---|---|---|---|---|
| K01 | Én webløsning til administratorer og forældre, installerbar som PWA på iOS og Android | Samme data på desktop og mobil. Appen kan installeres. | 01, 09, 14 | Åben |
| K02 | Forælderen skal have en udfyldt profil før tilmelding | Serveren afviser tilmelding, når profilen mangler felter, også selvom browseren omgås. | 02, 07 | Åben |
| K03 | Profil: fulde navn, adresse, telefon, e-mail, trøjestørrelse, væskebilletter, tivolibilletter, barnets navn og klasse | Felterne gemmes, valideres og kan rettes af ejeren. | 02 | Åben |
| K04 | Tivolibilletter højst 2 **ark** | 0, 1 og 2 accepteres. 3 afvises af serveren og databasen. | 02 | Åben |
| K05 | To roller: Administrator og Bruger | En bruger får "Ingen adgang" på admin-sider og ser aldrig andres profiler. | 01, 02, 15 | Åben |
| K06 | Admin opretter, retter og sletter begivenheder og vagter | CRUD med validering. Sletning med aktive tildelinger er blokeret. | 03, 04 | Åben |
| K07 | En vagt har et tidsrum og et maksimalt antal personer | Slut efter start, kapacitet ≥ 1, aldrig flere aktive tildelinger end kapaciteten. | 04, 05, 07 | Åben |
| K08 | Admin bestemmer, hvornår der åbnes for tilmelding | Knappen **Åbn for tilmelding** med en bekræftelse af periode og antal vagter. | 06 | Åben |
| K09 | Brugeren ser sine egne vagter i en personlig kalender | Også vagter, admin har tildelt i det skjulte, men kun for ejeren. | 05 | Åben |
| K10 | Nye vagter er skjulte (kladde) | Kladder ses aldrig af brugere: sider, realtid, push og e-mail. | 04, 06, 08, 10, 15 | Åben |
| K11 | Admin kan forhåndstildele skjulte vagter til brugere og sig selv | Modtageren ser vagten. Andre forældre ser den ikke. | 05 | Åben |
| K12 | Frigivelse gør vagterne synlige for alle på samme tid | Én bekræftet handling. Åbne sider opdateres via realtid. | 06, 08 | Åben |
| K13 | Først-til-mølle, beskyttet i databasen | 50 samtidige forsøg på én ledig plads giver præcis én tildeling. | 05, 07 | Åben |
| K14 | Brugeren kan vælge flere vagter og bekræfte samlet | Én POST, alt-eller-intet, forståelig besked ved konflikt. | 07 | Åben |
| K15 | Brugeren ser sine tilmeldte og tildelte vagter | Liste og kalender med tid, sted og oprindelse. | 05, 07 | Åben |
| K16 | Ingen afmelding, bytte eller overdragelse for brugere | Ingen knap og ingen action, der kan det. | 05, 15 | Åben |
| K17 | Kun admin kan fjerne en bruger fra en vagt | Pladsen bliver ledig igen, eller vagten kan slettes. Historikken bevares. | 05, 08 | Åben |
| K18 | Push ved frigivelse til iOS og Android | Web push (VAPID) til brugere, der har sagt ja på deres enhed. | 10 | Åben |
| K19 | Påmindelse 48 timer før vagten | Én pr. tildeling og kanal, aldrig dobbelt. | 12 | Åben |
| K20 | Påmindelse 24 timer før vagten | Som K19, også ved genstart og skift til/fra sommertid. | 12 | Åben |
| K21 | Baggrundsjob, der scanner én gang i timen | Intervallet står i konfigurationen. Status kan ses af admin. | 12, 13 | Åben |
| K22 | Webmail til systemets egen postkasse (brugerens krav 2026-11-15, ikke i PDF'en) | Et svar på en mail fra systemet kan læses og besvares af admin i **Indbakke**. Admin kan også skrive nye mails og slette. Kun admin har adgang. | 11, 18 | Åben |

Status: `Åben` → `Implementeret (step NN)` → `Godkendt (step 17)`.

## Ufravigelige forretningsregler

1. Kun rollerne **Administrator** og **Bruger**. Kun administratorer opretter brugere.
2. Nye vagter er altid **Kladde (skjult)**, uanset hvad en formular sender. Kladder optræder aldrig i ledige vagter, realtidsbeskeder, push eller e-mail.
3. Admin kan tildele kladdevagter. Kun den tildelte bruger ser sin egen vagt før frigivelsen, og kun den vagt, ikke resten af planen.
4. Frigivelse kræver bekræftelse og sker i én transaktion. Derefter afgør serverens transaktionsrækkefølge, hvem der får de ledige pladser (først-til-mølle). Kapaciteten tjekkes aldrig kun i browseren.
5. En tilmelding til flere vagter er **alt-eller-intet**. Ved konflikt oprettes ingen af tildelingerne, og brugeren skal vælge igen.
6. En tildeling er **låst**. Brugeren kan ikke afmelde, bytte eller give en vagt videre. Kun administratoren kan fjerne en bruger fra en vagt.
7. Påmindelser 48 og 24 timer før er idempotente: aldrig to ens beskeder, heller ikke ved genstart eller parallelle job.
8. Persondata (navn, adresse, telefon, e-mail, barn og klasse, trøjestørrelse og billetter) vises kun for ejeren og for administratorer. Push og realtidsbeskeder indeholder aldrig persondata.
9. Tivolibilletter: højst 2 ark.

## Afklaringer og antagelser

1. **Native apps → PWA.** iOS kan kun modtage web push fra iOS 16.4, og kun når appen er føjet til hjemmeskærmen. Det står i manualen.
2. **"Præcis" 48/24 timer vs. scanning hver time.** Påmindelsen sendes ved den første scanning efter tidspunktet, altså op til ét interval (60 min.) senere. Intervallet kan konfigureres. Browserens push-levering kan forsinke yderligere. Manualen beskriver det ærligt.
3. **Først-til-mølle** betyder den rækkefølge, serveren gennemfører transaktionerne i, ikke hvem der trykkede først på sin telefon.
4. **Én skole eller klasse.** Ingen multitenant, ingen betaling. Billetfelterne er ønsker i profilen, ikke et salg.
5. **Antagelser, som kan rettes:** Brugernavnet er forælderens e-mailadresse. Trøjestørrelse vælges fra en fast liste (XS–3XL). Væskebilletter er et antal stk. (0–50). Klassen er fritekst (fx "6.A"). Der findes kun statusserne Kladde og Åben.
6. **Mockups viser ting, der ikke er krav, og som ikke bygges:** marketingmenuen (Funktioner, Sådan virker det, For skoler og klasser, Hjælp), Beskeder, Rapporter, to-trinsbekræftelse, aktive sessioner, "Ansvarlig" pr. vagt, gruppetildeling ("9.A – Forældre"), trendtal ("siden sidste uge"), tilmeldingsfrist og "Kom i gang"-selvregistrering. Der må ikke være døde menupunkter eller knapper.
7. Eksempeldata i mockups (navne, årstal, tal) er dummydata og bruges ikke.

## Mockups

| Billede | Skærm | Steps | Bruges til |
|---|---|---|---|
| [01_forside.png](mockups/01_forside.png) | Forside og PWA | 01, 09 | Login-siden: kort forklaring, **Log ind** og "Installér på din enhed". Ingen offentlig forside, ingen "Kom i gang". |
| [02_admin_dashboard.png](mockups/02_admin_dashboard.png) | Admin-dashboard | 13 | KPI-kort, kommende begivenheder med fremdrift, seneste aktivitet. Kun rigtige tal. |
| [03_admin_begivenheder_og_vagter.png](mockups/03_admin_begivenheder_og_vagter.png) | Begivenheder og vagter | 03, 04, 06 | Begivenhedsliste, vagttabel med statusbadges, **Åbn for tilmelding** med bekræftelse. |
| [04_admin_opret_vagt.png](mockups/04_admin_opret_vagt.png) | Opret vagt | 04, 05 | Formularens layout og opsummering. Forhåndstildeling sker på vagtens side, ikke i oprettelsesformularen. |
| [05_bruger_ledige_vagter.png](mockups/05_bruger_ledige_vagter.png) | Ledige vagter | 07 | Begivenhedskort, tabel med afkrydsning, panelet **Valgte vagter** og **Bekræft tilmelding**. |
| [06_bruger_min_kalender.png](mockups/06_bruger_min_kalender.png) | Mine vagter og kalender | 05, 14 | Månedskalender, kommende/afholdte vagter, vagtdetaljer, mobilvisning med bundmenu. |
| [07_bruger_min_profil.png](mockups/07_bruger_min_profil.png) | Min profil | 02, 10, 11 | Profilformularen, push/e-mail-valg, **Skift adgangskode**. |

Designtokens (fra ChatGPT-pakkens designguide): primær blå `#1764B8`, mørk tekst `#13243D`, baggrund `#F6F9FD`, kort `#FFFFFF`, kant `#DFE6EF`, svag blå `#EAF2FD`. Status: kladde grå, åben grøn, advarsel rav, fejl rød. Farve er aldrig den eneste statusindikator. Dansk sprog og 24-timers tid i `Europe/Copenhagen`.

## Negative tests, der altid skal bestå

1. Bruger A kan ikke se en skjult vagt, der er tildelt bruger B, heller ikke ved at gætte id'et i en URL.
2. En åben begivenhed viser aldrig sine kladdevagter.
3. 50 samtidige tilmeldinger til én ledig plads giver præcis én aktiv tildeling.
4. To samtidige multivalg med overlap giver ingen overbooking og ingen requests, der hænger.
5. Et multivalg med tre vagter, hvor én er blevet fuld, giver ingen nye tildelinger.
6. En bruger kan ikke afmelde sig, heller ikke ved at sende en POST direkte.
7. To samtidige påmindelsesscanninger eller en genstart sender ikke den samme påmindelse to gange.
8. Push uden tilladelse eller i en browser uden understøttelse giver en forklaring og ingen fejl.
9. Realtidsbeskeder, push og e-mail afslører aldrig en kladdevagt.
