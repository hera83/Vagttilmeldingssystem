# STEP 09 – Først-til-mølle og batchtransaktioner

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Implementér kernen i systemets korrekthed under stor samtidighed: en bruger må aldrig få en plads, som ikke findes.

## Konkrete implementeringsopgaver

- API `POST /api/enrollments/batch` med unikke ShiftIds, valideret og fuldført profil, auth og korrekt Open-status.
- I **én SQL Server-transaktion**, lås alle berørte Shift-rækker i deterministisk ID-rækkefølge (fx UPDLOCK/HOLDLOCK eller SERIALIZABLE med gennemtestet korrekthed), beregn antal aktive tildelinger, afvis hvis fyldt/duplikat/ikke-åben. Ingen delvis commit.
- Brug unique index på aktive assignments og korrekt håndtering af SQL Server deadlock/retry med begrænset kontrolleret genforsøg. Ingen in-memory `lock` som eneste beskyttelse.
- Ved succes: commit samlet, audit/outbox-event og genberegnet status. Returner præcis hvilke vagter der blev booket.
- Ved konflikt: struktureret 409, klientvenlig besked og opdaterede kapaciteter, men **ingen** silent booking af subset.
- Booking må ikke kunne forudsiges via frontend check; API er autoritativt.
- Byg **rigtige** SQL Server integration-/konkurrencetests: 50 brugere → 1 plads, 2 pladser, parallelle batchkald med overlap, duplikat i samme request, allerede booket, delvist fyldt, samtidig admin-tildeling, samtidig frigivelse, samtidig admin-fjernelse.
- Dokumentér transaktionsgrænser i `docs/ARCHITECTURE.md`.

## Accept – dette trin er først BESTÅET, når

- Alle kapacitetstests består på faktisk SQL Server; ingen overbooking.
- Batch er alt-eller-intet for 2–3 vagter ved konflikt.
- Brugeren kan ikke booke kladder eller andres skjulte vagter.
- Backend tests dækker K13–K14.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_10_LEDIGE_VAGTER_BRUGERUI.md`**.
