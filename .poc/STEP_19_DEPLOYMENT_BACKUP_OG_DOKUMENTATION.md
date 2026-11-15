# STEP 19 – Opsætning, drift og backup

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Gør løsningen reproducerbar i test og klar til brugerens foretrukne selvhostede miljø, uden at ændre live servere uden GO.

## Konkrete implementeringsopgaver

- Opret produktionsbuild: ASP.NET Core publish, Vite frontend build, same-origin hosting, appsettings-template og env-vars for SQL Server, cookie secrets, push VAPID, mail (optionelt).
- Giv **to** dokumenterede hostingopskrifter i `docs/OPERATIONS.md`: (A) Windows/IIS og (B) Ubuntu med systemd/nginx eller Docker. Brugeren vælger én at afprøve; implementér ingen live deployment uden godkendelse.
- HTTPS, reverse-proxy forwarded headers, SignalR websocket support og service worker scope skal testes i valgt testmiljø.
- SQL Server migrations og backup/restore incl. smoke-test; dokumentér kontrol af fil-rettigheder, backup-retention, genstart, logs og monitorering.
- Dokumentér, hvordan admin oprettes sikkert, hvordan VAPID keys udstedes/roteres, og hvordan worker timers styres. Ingen hardcodede hemmeligheder.
- Lav install/upgrade/rollback-runbook og brugerhjælp til admin og frivillige; brugeren skal kunne drive systemet selv.
- Smoke-test efter deployment: health, login, CRUD, tilmelding, SignalR, push subscription og timelig workerstatus.

## Accept – dette trin er først BESTÅET, når

- Frisk testserver kan opsættes fra dokumentationen uden lokal udviklermaskines særlige stier.
- Genstart og backup/restore er prøvet eller markeret som specifik release-blocker.
- IIS og Ubuntu opskrifter findes; ingen uautoriseret deployment.
- Produktionen har ingen demo-user/password.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_20_END_TO_END_GODKENDELSE.md`**.
