# STEP 18 – Manuel end-to-end brugeraccepttest

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Afvikl en realistisk skolefestcase med både administrator og flere forældre. Dette er en **test**, ikke en præsentation.

## Konkrete implementeringsopgaver

- Opret udviklingstestdata gennem idempotent `DevOnly` seed: 1 admin, mindst 3 forældre med fuldstændige profiler og 1 begivenhed "Skolefest" med 6–8 vagter.
- Administration: opret alle som skjulte, tildel en skjult vagt til forælder A, verificér kalenderen hos A og skjul hos B/C.
- Frigiv med bekræftelse. Åbn parallelt på desktop og telefon; verificér live oversigt og korrekt kapacitet.
- Bruger B vælger flere vagter, bekræfter, ser dem i egen kalender. Brug C i konkurrencetest om sidste plads og verificér 409/håndtering.
- Simulér sygdom: B kan ikke afmelde; admin fjerner booking og plads bliver tilgængelig igen.
- Afvikl reminder-job med testur og sandbox push/email. Tjek 48/24 samt ingen dobbeltudsendelse.
- Udfør korrektionsrunde: fejl rettes i dette step med regressionstests. Udrul ikke, hvis væsentlige fejl er åbne.
- Opret `docs/UAT_REPORT.md` med resultater og skærmbilleder, ikke kun checklistesymboler.

## Accept – dette trin er først BESTÅET, når

- Alle scenarier er dokumenteret PASS eller ærligt BLOCKER/FAIL.
- Ingen vigtig handling kræver direkte database-manipulation i normal UI.
- Realistisk case demonstrerer PDF'ens særlige regler.
- POC er kun kandidat til deployment ved grøn UAT.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_19_DEPLOYMENT_BACKUP_OG_DOKUMENTATION.md`**.
