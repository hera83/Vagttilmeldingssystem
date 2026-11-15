# STEP 06 – Vagter, kapacitet og skjulte statusser

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Lever administrativ oprettelse af vagter og det sikre kladde-flow.

## Konkrete implementeringsopgaver

- CRUD for Shift med event, navn/opgave, dato, start/slut, kapacitet og praktisk beskrivelse.
- **Default status Draft/Skjult** uanset API-klient; client kan ikke sætte Open som en usikret create-sideeffekt.
- Admin kan i liste/kalender se status Kladde/Skjult/Åben og belægning; tilføj søgning og statusfilter.
- Tjek tidsvalidering, eventperiode (eller dokumentér tilladt afvigelse), positive maksimumpladser og om kapacitet sænkes under antal bookede.
- En vagt med aktive tildelinger må ikke slettes/ændres på en måde, der gør tildelinger meningsløse uden kontrolleret adminproces.
- Brugerens API skal skjule kladder fuldstændigt. Test direkte endpoint-adgang og søgning.
- Brug `mockups/04_admin_opret_vagt.png` til formularens layout.

## Accept – dette trin er først BESTÅET, når

- Nyoprettet vagt er altid skjult.
- Kun admin finder den i adminlisten; almindelig bruger gør ikke.
- Datavalidering hindrer negative kapaciteter og ugyldige tidspunkter.
- Tests dækker K06, K07, K10.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_07_FORHAANDSTILDELING.md`**.
