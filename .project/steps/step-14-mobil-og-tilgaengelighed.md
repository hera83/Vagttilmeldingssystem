# Step 14 - Mobil, design og tilgængelighed (gennemgang)

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 13, Step 18 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
En samlet gennemgang af alle sider, så appen føles som ét system på desktop og telefon, kan bruges med tastatur og skærmlæser og ligner mockups, hvor det giver mening. Krav: K01.

## Omfang
- **Med:**
  - Alle sider ved 360 px, 390 px og 1280 px bredde: ingen vandret scroll, tabeller bliver til kort på mobil, sticky **Bekræft tilmelding**, bundmenu for brugere og sammenklappelig admin-menu.
  - Fælles komponenter i `site.css`: knapper, felter, statusbadges (tekst + ikon, ikke kun farve), kort, tabeller, beskeder (succes/fejl med `role="status"`/`role="alert"`), tomme tilstande.
  - Tilgængelighed mod WCAG 2.2 AA: labels på alle felter, synligt fokus, tastaturbetjening af multivalget og kalenderen, `aria-live` på realtidsopdateringer og valideringsfejl, kontrast, sprog `da`, meningsfulde sidetitler, "Spring til indhold"-link.
  - Dansk sprog og 24-timers tid overalt. Tjek af tomme, fejl-, offline-, 404- og "Ingen adgang"-sider.
  - Gennemgang af de 7 mockups mod de færdige sider. Afvigelser skrives i projektlaget med begrundelse.
  - Ingen døde links eller knapper.
  - Skærmbilleder til manualen (desktop og mobil). Login-siden og andre anonyme sider med `msedge --headless`. Sider med login: enten med Playwright for .NET i `web.Tests` (kræver `playwright install`, aftales med brugeren i steppet) eller sat på **Manglende billeder**.
- **Ikke med:** Nye funktioner.

## Plan
1. [ ] Gennemgå hver side (login, forside, **Ledige vagter**, **Mine vagter**, **Min profil**, **Installér appen**, dashboard, begivenheder, vagt, tildeling, frigivelse, brugere, **Indbakke**, fejl/status) ved de tre bredder, og ret fundene.
2. [ ] Ryd op i `site.css` (tokens, komponenter, mobil-breakpoints) og flyt eventuel inline-JS til `wwwroot/js/` (forberedelse til CSP i step 15).
3. [ ] Tastatur- og skærmlæsertest (fx Windows Fortæller eller NVDA) af login, multivalg og bekræftelse.
4. [ ] Beslut med brugeren, om skærmbilleder med login tages med Playwright. Tag skærmbillederne.
5. [ ] Opdatér `web/.claude/CLAUDE.md`: afsnit om designsystemet og afvigelser fra mockups.
6. [ ] Opdatér manualen: alle skærmbilleder, **Manglende billeder**, ændringslog og `.manuals/README.md`.
7. [ ] Opdatér Status for K01 i `.project/krav/krav.md`.
8. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 14 - Mobil, design og tilgængelighed (gennemgang)" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/views/**` og `web/wwwroot/css/site.css`, `web/wwwroot/js/` (ændret)
- evt. `web.Tests/` (Playwright-skærmbilleder)
- `.manuals/web/images/` (billeder)

## Beslutninger
- Bekræftelser er egne sider eller `<dialog>` med fokusfælde, så de virker med tastatur (foreslået af Claude).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt i Edge DevTools med mobilvisning (360 px) og på desktop. Tastatur-gennemgang af hovedflowet.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Skal Playwright for .NET bruges til skærmbilleder af sider med login? Afgøres i steppet.
