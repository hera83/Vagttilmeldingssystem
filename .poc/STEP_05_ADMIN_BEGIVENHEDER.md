# STEP 05 – Administratorstyring af begivenheder

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Lever administrationssider og API til reelle begivenheder, som vagter kan ligge under.

## Konkrete implementeringsopgaver

- CRUD for begivenhed: navn, beskrivelse, start/slut/periode og lokation; validering i backend.
- Admin-overblik med søgning, filtrering og navigation til vagter; tom tilstand og fejltilstande.
- Beslut sletningsregler: begivenheder med aktive tildelinger må ikke bare slettes og miste historik; brug arkiv/status og tydelig bekræftelse.
- Frontend med enterprise tabeller/kort som i `mockups/03_admin_begivenheder_og_vagter.png`.
- Test permissions, inputvalidering og at private kladder ikke bliver offentliggjort ved at oprette en begivenhed.
- Registrér ændringer i audit trail uden at logge persondata unødigt.

## Accept – dette trin er først BESTÅET, når

- Admin kan oprette, redigere, arkivere og finde events.
- Bruger kan ikke mutere admin-events.
- Eventoprettelse udløser ikke frigivelse eller push.
- E2E-smoketest opret event → se event i adminliste.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_06_ADMIN_VAGTER_OG_KLADDER.md`**.
