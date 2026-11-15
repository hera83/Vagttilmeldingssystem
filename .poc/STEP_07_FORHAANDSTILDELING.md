# STEP 07 – Private, låste admin-tildelinger

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Byg forhåndstildeling inden frigivelse – et af originalprojektets vigtigste særkrav.

## Konkrete implementeringsopgaver

- Admin kan søge efter registreret bruger og tildele ledig plads på skjult vagt (eller sig selv).
- Bookingkapacitet gælder også admin-tildelinger; duplikater må ikke kunne opstå.
- Tildelt skjult vagt bliver synlig i **den pågældendes egen** /api/me/shifts og kalender; alle andre ser ikke vagtens eksistens.
- Tilføj tydelig ejerbesked på egen kalender ("Tildelt af administrator") uden at afsløre resten af eventets skjulte plan.
- Må ikke sende bred offentlig push, før administrator frigiver; privat notifikation til modtager kræver særskilt kontrolleret valg og må ikke afsløre andre detaljer.
- Backend skal håndhæve rettigheder, kapacitet og ejerskab; UI alene er ikke nok.
- Test admin-selvtildeling, duplicate tildeling, fuld vagt og forsøg fra anden bruger.

## Accept – dette trin er først BESTÅET, når

- Bruger A ser sin admin-tildelte skjulte vagt; Bruger B ser den ikke.
- Ikke-admin kan ikke forhåndstildele.
- Hidden/admin-assign påvirker ledig kapacitet korrekt.
- Tests dækker K09–K11.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_08_FRIGIVELSE_OG_REALTIME.md`**.
