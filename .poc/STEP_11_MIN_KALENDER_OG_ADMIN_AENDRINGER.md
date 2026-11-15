# STEP 11 – Personlig kalender og låste tildelinger

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Lad brugeren følge sine vagter, også dem admin tildelte før frigivelse. Implementér admin-only ændringsflow uden byttefunktion.

## Konkrete implementeringsopgaver

- Byg "Mine vagter" og kalender, kommende/tidligere, detaljer og mødetid/lokation.
- Hent fra server med private ejer-scope; inkludér skjulte egne tildelinger, men ingen andre skjulte vagter.
- Markér oprindelse (selvvalgt/admin-tildelt) og at vagter er låst. Implementér **ingen** afmeldings-, bytte-, overdragelses- eller 'giv videre'-knap til brugere.
- Admin kan fremsøge assignment, fjerne via kontrolleret action og bekræftelse, og vælge om vagt holdes åben/lukkes. Registrér ændringen i audit.
- Efter admin-fjernelse opdateres ledige pladser og personens kalender, inkl. SignalR/outbox.
- Eksport til kalender/ICS kan være en lille **valgfri** forbedring, hvis kravene allerede er stabile.
- Brug `mockups/06_bruger_min_kalender.png`.

## Accept – dette trin er først BESTÅET, når

- Bruger kan se sine egne skjulte og offentlige tildelinger.
- Bruger kan **ikke** afmelde/bytte (API 403/405 afhængigt af endpoint).
- Kun admin kan fjerne booking og genåbne kapacitet.
- Tests dækker K09, K15–K17.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_12_PWA_WEBPUSH_OG_NOTIFIKATIONER.md`**.
