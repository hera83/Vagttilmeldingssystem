# STEP 12 – Installerbar PWA og push ved frigivelse

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Byg én PWA til browser og mobil med rigtige service workers, abonnementer og push – med realistiske platformgrænser.

## Konkrete implementeringsopgaver

- Vite PWA plugin/webmanifest med navn, ikoner, start_url, `display: standalone`, theme colors, korrekt cachestrategi og install-flow.
- Tilføj service worker til web push/notification-click, HTTPS-krav og nyttig `installér app`-vejledning, herunder iOS 'Føj til hjemmeskærm'.
- Brug VAPID; opret/forny/slet PushSubscription for autentificeret bruger (endpoint+keys gemmes forsvarligt), læg aldrig private keys i repo/frontend.
- Bed om notifikationstilladelse **efter en brugerhandling**, ikke ved første page load. Vis klar status: aktiv/deaktiveret/ikke-understøttet.
- Outbox processing sender push efter frigivelse til relevante tilmeldte/modtagere ud fra private regler; for bred notifikation om nye åbne vagter send generisk tekst uden følsomme oplysninger.
- Fejlslagne/udløbne abonnements-endpoints håndteres og ryddes; registrér forsøgsstatus og eventuel retry.
- Cache kun app shell og egnede statiske assets, ikke private data eller tilmeldingsresultater; booking kræver online.
- Test på mindst én Android-browser og iOS Safari installeret som PWA, hvis enheder er tilgængelige. Hvis ikke: dokumentér manuel testvejledning og ikke-testede platforme, påstå ikke fuld dækning.

## Accept – dette trin er først BESTÅET, når

- PWA kan installeres på understøttede browsere, også uden native apps.
- Push sendes via outbox til brugere med aktivt samtykke og understøttet platform.
- Offline kan brugeren ikke tilmelde sig vagter; UI siger det klart.
- Tests dækker K01 og K18, inklusive unsupported/browser-denied cases.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_13_PAAMINDELSER.md`**.
