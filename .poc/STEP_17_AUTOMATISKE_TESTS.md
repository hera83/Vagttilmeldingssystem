# STEP 17 – Komplet test suite og CI

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Konsolidér alle tests til gentagelig pipeline; en fungerende POC skal kunne bygges og valideres fra en ren checkout.

## Konkrete implementeringsopgaver

- Unit tests for status, valideringer, tid, billetark og kapacitet.
- Integrationstest mod **reel SQL Server** for bruger/rolle, skjult/forhåndstildelt, frigivelse, batchtransaction, admin overrides og idempotente reminders.
- Playwright E2E for registrering→profil→admin-event→skjult vagt→forhåndstildeling→frigivelse→flere bookinger→egen kalender→admin-fjernelse.
- Race tests: 50+ samtidige bookings om sidste plads; parallel multivagt; worker-reminders parallelt. Testen fejler ved enhver overbooking.
- Frontend `lint`, TS check, build, PWA manifest/sw check, responsive screenshots og smoke navigation.
- Et script (`scripts/verify-all.sh`, PowerShell-variant hvis relevant) starter testdependences, kører tests og giver entydig exit code. CI workflow er ønskeligt, men må ikke få fat i live secrets.
- Opret `docs/TESTS.md` med præcise trin og miljøkrav; ingen påstand om grøn CI uden udførelse.

## Accept – dette trin er først BESTÅET, når

- Ét lokalt kommando-flow kan reproducere hele testpakken.
- Alle kritiske K01–K21-testcases er mappet i requirements matrix.
- Konkrete konkurrencetests består på SQL Server.
- Alle kendte testfejl og utestede platforme er rapporteret.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_18_REALISTISK_POC_BRUGERTEST.md`**.
