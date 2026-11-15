# STEP 20 – Endelig kravkontrol og overdragelse

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Projektet er først færdigt, når alle centrale funktioner er implementeret, testet og dokumenteret. Lav ingen kosmetisk slutstatus, hvis systemet ikke virker.

## Konkrete implementeringsopgaver

- Gennemgå K01–K21 én for én og angiv: implementeret fil/endpoint/UI, automatisk test, manuel test, status.
- Genkør fuld verificering mod clean DB inkl. migrationer, byg, auth, admin, privat skjult forhåndstildeling, publicering, race-safe multibooking, kalender, PWA, push, påmindelser, låste vagter og admin-fjernelse.
- Verificér begge roller på desktop og mobil, baseret på de syv mockups og korrekt adgangsmodel.
- Tjek, at setup-, backup-, restore- og rollback-dokumentation er tilstrækkelig til en ny opsætning.
- Lav `docs/FINAL_HANDOVER.md`: arkitektur, kørselsvejledning, login/admin-oprettelse, miljøvariabler, tests, eventuelle kendte mangler og konkrete risici, logplacering og supportplan.
- Ryd dev seeds og placeholder links fra produktionsopsætning. Bekræft at `CLAUDE.md` og `STEP_*.md` ikke er tracked i Git.
- Markér systemet **FÆRDIGT** kun hvis alle obligatoriske tests og UAT er PASS og en reel installation er afprøvet. Ellers afslut som IKKE KLAR med blocker-liste og konkrete reparationsopgaver.

## Accept – dette trin er først BESTÅET, når

- K01–K21 har dokumenteret PASS eller står som åben, ærligt rapporteret blocker.
- Hele bookingforløbet er vist på en ny testinstallation.
- Der er en komplet handoverpakke til almindelig drift.
- Ingen ord som 'driftsklar' uden reel deployment-/restore-verifikation.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`Ingen – afslutning og evt. efterfølgende forbedringer med nyt GO`**.
