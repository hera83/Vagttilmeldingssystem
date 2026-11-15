# Step 13 - Admin-dashboard og deltagerliste

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 12 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Administratoren får ét overblik over begivenheder, pladser, aktivitet og påmindelser, bygget på rigtige tal, og kan hente en deltagerliste til den praktiske planlægning (trøjer, billetter, kontakt). Krav: K21 (status), støtte til K06/K08/K17.

## Omfang
- **Med:**
  - `DashboardController` (`AdminOnly`), layout efter mockup 02. Admins forside (`/`) sender til dashboardet.
  - **KPI-kort:** aktive begivenheder, åbne vagter, besatte/ledige pladser med belægning i %, påmindelser sendt de seneste 7 dage, brugere uden komplet profil. Ingen trendtal ("siden sidste uge"), fordi der ikke er historik til dem.
  - **Kommende begivenheder:** dato, vagter, besat/i alt med fremdriftsbjælke og status (har kladder / åben).
  - **Seneste aktivitet**, bygget af tidsstempler: tilmeldinger, admin-tildelinger, fjernelser og frigivelser. Kun navne, ikke adresser eller børn.
  - **Beskeder og påmindelser:** status fra `ReminderWorkerStatus` (seneste/næste scanning, seneste fejl) og antal `Failed` i køen med seneste fejltekst.
  - **Genveje:** **Opret begivenhed**, begivenheder med kladder (→ **Åbn for tilmelding**), vagter med ledige pladser.
  - **Deltagerliste (CSV)** pr. begivenhed fra `Events/Details`: vagt, dato/tid, navn, telefon, e-mail, trøjestørrelse, væskebilletter, tivoliark, barnets navn og klasse. UTF-8 med BOM og semikolon, så den åbner korrekt i dansk Excel. Kun admin.
  - Menupunktet **Dashboard** øverst i admin-menuen.
- **Ikke med:** Rapporter, grafer over tid, "Send påmindelser manuelt" (ikke krav).

## Plan
1. [ ] `managers/Dashboard/` (`DashboardManager`) med læsemetoder i de berørte repositories (tællinger og aktivitet som projektioner, `AsNoTracking`).
2. [ ] `DashboardController` + `viewmodels/Dashboard/` + `views/Dashboard/Index.cshtml`. Forsiden for admin sender videre.
3. [ ] CSV-eksport i `EventsController` (action `Participants`) via en manager-metode og en lille CSV-hjælper i `extensions/`.
4. [ ] Tests: tallene svarer til databasen efter tilmelding og fjernelse. En bruger uden admin får "Ingen adgang" på dashboard og CSV. CSV'en har de rigtige kolonner og escaper semikolon/anførselstegn. Ingen kladder tælles som åbne.
5. [ ] Opdatér `web/.claude/CLAUDE.md`: Managers, Controllers og endpoints, Extensions.
6. [ ] Opdatér manualen: **Dashboard** og **Deltagerliste**, skærmbillede (eller Manglende billeder), ændringslog og `.manuals/README.md`.
7. [ ] Opdatér Status for K21 i `.project/krav/krav.md`.
8. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 13 - Admin-dashboard og deltagerliste" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/managers/Dashboard/`, `web/controllers/DashboardController.cs`, `web/viewmodels/Dashboard/`, `web/views/Dashboard/` (nye)
- `web/repositories/*` (læsemetoder), `web/controllers/EventsController.cs`, `web/controllers/HomeController.cs`, `web/views/Shared/_Layout.cshtml`, `web/extensions/` (ændret)

## Beslutninger
- Kun tal, der kan beregnes af data. Mockuppens trendtal og "Send påmindelser til 32 deltagere" bygges ikke (krav.md, afklaring 6).
- Deltagerlisten er den eneste eksport af persondata og kun for admin. Den logges som en hændelse (uden persondata) (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt: se dashboardet før og efter en tilmelding. Hent deltagerlisten og åbn den i Excel (æ, ø og å skal vises korrekt).

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
