# STEP 08 – Publicering og synkronisering

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Gør en bekræftet publicering til en sikker og synkron operation med korrekt synlighed på web og mobil.

## Konkrete implementeringsopgaver

- Administrator ser tydelig "Åbn for tilmelding"-handling med bekræftelsesdialog: begivenhed/periode, antal kladder, konsekvens.
- Implementér admin endpoint til atomisk frigivelse af valgte kladder. Afvis tomt/fejlagtigt scope; brug adgangstjek, idempotens og audit.
- Publicering overgår fra Draft til Open og placerer outbox-registrering **inden for samme DB-transaktion**.
- SignalR broadcaster alene **offentlige ændringer efter commit**. Client skal hente fresh snapshot ved tilslutning/genforbindelse, så der ikke opstår afhængighed af perfekte websockets.
- Realtime-UI opdaterer antal ledige pladser og frigivne vagter uden hard-refresh, men ved API-fejl vises klar information.
- Udvid tests med to brugere i forskellige sessioner og et event, der frigives midt i visningen.
- Forbered outbox-event til STEP_12 (push) uden at fake en sendt notifikation.

## Accept – dette trin er først BESTÅET, når

- Før publicering er vagten privat; efter publicering vises den til Bruger B straks ved opdatering og normalt via realtid.
- Ingen SignalR- eller outbox-lækage af kladdetitler til uautoriserede.
- Gentagne publiceringskald skaber ikke dobbelthændelser.
- Tests dækker K08, K12.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_09_ATOMISK_BOOKINGMOTOR.md`**.
