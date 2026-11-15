# STEP 14 – Administratoroverblik og aktivitet

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Byg et virkeligt admin dashboard baseret på faktiske data, ikke demo-tal fra billedet.

## Konkrete implementeringsopgaver

- KPI-kort: antal aktive events, åbne vagter, besatte/ledige pladser, udsendte påmindelser og belægningsgrad.
- Kommende begivenheder, progressbars, seneste **auditerede** aktiviteter og handlinger (opret event, se kladder, åbn for tilmelding, håndtér ændringer).
- Undgå falske trend-kort (fx 'siden sidste uge') uden faktisk historik; lav ægte queries hvis trend vises.
- Admin kan filtrere på dato/event. Undgå at vise brugeres adresser eller børn i dashboardets offentlige oversigt.
- Lav en enkel udtræksfunktion (CSV) til admin ved behov, med autorisationscheck og kun relevante kontaktoplysninger; marker dette som understøttende UI-funktion.
- Brug `mockups/02_admin_dashboard.png`, men lad rigtige tal og regler bestemme interface.

## Accept – dette trin er først BESTÅET, når

- Stats svarer til databaseantallet og opdateres efter bookinger/fjernelse.
- Dashboard er kun tilgængeligt for administrator.
- Først-til-mølle-begrænsninger forbliver intakte.
- Tests dækker rapportering og adminrolle.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_15_RESPONSIVT_DESIGN_OG_TILGAENGELIGHED.md`**.
