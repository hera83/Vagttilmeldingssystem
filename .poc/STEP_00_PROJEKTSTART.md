# STEP 00 – Afklaring af scope og styring

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Læs hele PDF'en og kravdokumentet, alle syv mockups og teknisk baseline. Gennemgå projektmappen og noter, om der allerede findes kildekode; hvis den er tom, opret kun styringsfiler. Frys krav-ID'erne K01–K21 og registrér usikkerheder uden at improvisere ekstra features.

## Konkrete implementeringsopgaver

- Opret `docs/STEP_STATUS.md` som tabel: STEP, status (Ikke startet/I gang/BESTÅET/Blokeret), testresultat, dato, noter. Alle steps starter som Ikke startet.
- Opret `docs/ARCHITECTURE.md` med målarkitektur og beslutninger om single-institution, SQL Server, API, PWA og deployment.
- Opret `docs/decisions/0001-stack.md` med klare alternativer og hvorfor den valgte baseline er valgt.
- Opret `docs/requirements-traceability.md` med K01–K21 knyttet til relevante steps og senere tests.
- Undersøg miljøforudsætninger (.NET SDK, Node, SQL Server/Docker, certifikater) uden at installere uanmodet software eller eksponere secrets.
- Opret `.gitignore` med blokering af `/CLAUDE.md`, `/.claude/`, `/STEP_*.md`, `/PROMPTS_TIL_CLAUDE_CODE.md`, `.env`, private appsettings, build outputs og testdata. Brug ikke Git til Claude-instruktionerne.
- Fortæl tydeligt at login, data og push endnu **ikke** er bygget.

## Accept – dette trin er først BESTÅET, når

- Vis relevante versionskommandoer uden at påstå installationer er gjort.
- `docs/STEP_STATUS.md` indeholder samtlige trin, og requirements matrix indeholder K01–K21.
- `.gitignore` beskytter både secrets og Claude-instruktioner.
- Ingen produktionskode behøver eksistere endnu.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_01_LOSNING_OG_OPSTART.md`**.
