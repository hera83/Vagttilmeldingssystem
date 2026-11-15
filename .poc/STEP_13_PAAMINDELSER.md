# STEP 13 – 48- og 24-timers automatiske påmindelser

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Byg stabil baggrundskørsel i serveren, der sender påmindelser 48 og 24 timer før. Vær eksplicit om at timelig scanning ikke garanterer præcis minut-levering.

## Konkrete implementeringsopgaver

- Indfør `ReminderDelivery` med unik `(AssignmentId, ReminderType)` og status; due time baseret på Shift.StartUtc minus 48/24 timer.
- Hosted worker/scanner **hver time som PDF'en kræver**; interval skal være konfigurerbart for senere behov. Processér kun aktive assignments og huskændringer (flyttet tid => ny due-beregning og fornuftig policy for allerede afsendt).
- Arbejd med robuste outbox rows og idempotent claim/lease, så flere worker-instanser/genstart ikke forårsager dobbeltudsendelse.
- Push er primær notifikationskanal. E-mail kan tilføjes som frivillig fallback, men må ikke være forudsætning for at hele PWA'en fungerer.
- Log ikke fuldt persondata eller push keys; registrér leveringsstatus og fejl; giv admin sundhed/overblik.
- Implementér test med fiktivt ur og cases: 48h, 24h, for sent indenfor interval, genstart, duplikat scan, annullering, ændret vagt, DST i Danmark, manglende abonnement.
- Dokumentér: Push er best effort; en timelig scanner giver potentielt ~0–60 min forskydning og browserspecifik leveringsforsinkelse. Markér præcisionskonflikten i docs.

## Accept – dette trin er først BESTÅET, når

- Ingen dobbelte påmindelser ved to parallelle workers.
- 48h og 24h genererer separate, persisterede notifikationshændelser.
- Scanneren overlever restart og logger eventuelle fejl.
- Tests dækker K19–K21.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_14_ADMIN_DASHBOARD_OG_RAPPORTERING.md`**.
