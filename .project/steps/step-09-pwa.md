# Step 09 - PWA: installérbar app og offline-side

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 01 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Appen kan installeres på mobilens hjemmeskærm og føles som en app, uden App Store og Google Play. Service workeren er også forudsætningen for web push i step 10. Krav: K01.

## Omfang
- **Med:**
  - `wwwroot/manifest.webmanifest`: navn "Vagttilmelding", `short_name`, `start_url: "/"`, `display: "standalone"`, `theme_color #1764B8`, `background_color #F6F9FD`, `lang: "da"`, ikoner 192/512 (+ maskable) og `apple-touch-icon`.
  - Ikoner tegnet som SVG (kalenderlogoet) og konverteret til PNG (fx med `msedge --headless --screenshot`). Ingen ikoner fra nettet.
  - `wwwroot/service-worker.js` på roden (`/service-worker.js`): cacher kun statiske filer (css, js, ikoner) og `offline.html`. Sider hentes altid fra netværket (network-first), og uden net vises `offline.html`. Personlige sider og POST-svar caches aldrig.
  - Tags i `_Layout` (manifest, theme-color, apple-meta) og registrering af service workeren i `wwwroot/js/pwa.js`.
  - Offline: en bjælke "Du er offline – tilmelding kræver internet", og **Bekræft tilmelding** deaktiveres. Serveren er stadig den eneste, der afgør tilmeldingen.
  - Installationsvejledning: en sektion på login-siden (mockup 01: "Installér på din enhed") og siden `Home/Install` med Android-knap (`beforeinstallprompt`) og iOS-trin ("Del → Føj til hjemmeskærm").
- **Ikke med:** Push (step 10). Test på rigtige telefoner kræver https på en server (step 16).

## Plan
1. [ ] Ikoner (SVG → PNG) i `wwwroot/icons/`.
2. [ ] `manifest.webmanifest`, `service-worker.js`, `offline.html`, `js/pwa.js`. Tjek, at `MapStaticAssets` serverer `.webmanifest` med den rigtige content-type og at filerne er anonyme (login-siden skal kunne hente dem).
3. [ ] Layout-tags, offline-bjælke og spærring af knappen.
4. [ ] `Home/Install` + sektionen på login-siden.
5. [ ] Tests: `/manifest.webmanifest` og `/service-worker.js` svarer 200 uden login med de rigtige content-types. Manuelt i Edge/Chrome DevTools: manifest uden fejl, service worker aktiv, "Installér app" tilbydes, offline viser offline-siden.
6. [ ] Opdatér `web/.claude/CLAUDE.md`: Controllers og endpoints (`Home/Install`), afsnittet om `wwwroot/`.
7. [ ] Opdatér manualen: **Installér appen** (Android, iPhone, desktop), offline, skærmbilleder (eller Manglende billeder), ændringslog og `.manuals/README.md`.
8. [ ] Opdatér Status for K01 i `.project/krav/krav.md`.
9. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 09 - PWA: installérbar app og offline-side" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/wwwroot/manifest.webmanifest`, `web/wwwroot/service-worker.js`, `web/wwwroot/offline.html`, `web/wwwroot/js/pwa.js`, `web/wwwroot/icons/` (nye)
- `web/views/Shared/_Layout.cshtml`, `web/views/Account/Login.cshtml`, `web/controllers/HomeController.cs`, `web/views/Home/Install.cshtml` (ændret/ny)

## Beslutninger
- Ingen offline-data: booking og personlige sider kræver net, så der aldrig vises forældede pladser eller persondata fra cache (krav-pakken).
- Installationsvejledningen ligger på login-siden, fordi der ikke er en offentlig forside (step 01).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt i Edge: DevTools → Application → Manifest og Service workers, installér appen på desktop, slå netværket fra og se offline-siden.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Test på rigtig Android og iPhone kræver https (localhost tæller kun på samme maskine). Planen forbereder kun Docker (step 16), så det testes i step 17, hvis appen kører med https et sted. Ellers står det som ikke testet.
