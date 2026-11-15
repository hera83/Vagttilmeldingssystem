# STEP 10 – Ledige vagter og samlet tilmelding

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Byg brugerens primære flows mod den **færdige bookingmotor** – ikke mod hardcodede eksempeldata.

## Konkrete implementeringsopgaver

- Responsiv liste over **kun Open vagter** med event, dato/tid, kapacitet, ledige pladser, opgavebeskrivelse og filtrering.
- Tilføj multi-select af flere vagter med fast/tydelig "Valgte vagter"-opsummering og én bekræftelseshandling.
- Vis loading, fravær af ledige vagter, offline, udløbet session, manglende profil, kapacitet fuld og 409-konflikt. Gør genvalg eksplicit ved 409.
- Alle servermutationer går via anti-CSRF-beskyttet batch-API; ingen optimistisk 'du har fået pladsen' før commit.
- SignalR opdaterer kapacitet i baggrunden; afværg UI-race og invalider stale queries efter ændringer.
- Adskil admin-menu fra brugermenu, og design mobilkort ud fra `mockups/05_bruger_ledige_vagter.png`.
- E2E: bruger logger ind, vælger tre, bekræfter, ser nye vagter i Mine vagter.

## Accept – dette trin er først BESTÅET, når

- Ægte databasebogninger opstår ved bekræftelse og er synlige bagefter.
- Det er muligt at vælge flere vagter samlet på mobil og desktop.
- Ved konflikt viser UI en forståelig fejl uden delvis tilmelding.
- Tests dækker K14–K15.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_11_MIN_KALENDER_OG_ADMIN_AENDRINGER.md`**.
