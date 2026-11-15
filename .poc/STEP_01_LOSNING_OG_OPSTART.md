# STEP 01 – Opret startbar solution og frontend

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Opret en fungerende .NET solution og React/Vite/TypeScript-app med et enkelt dansk enterprise-shell. Dette er grundlaget for alle senere reelle features.

## Konkrete implementeringsopgaver

- Opret `src/Vagttilmelding.Api`, `Application`, `Infrastructure`, `Web`, samt testprojekter og dokumentationsmapper ifølge `TEKNISK_DESIGN.md`.
- Konfigurer `dotnet` solution, DI, exception handling via ProblemDetails, health endpoints, struktureret logging, OpenAPI i udviklingsmiljø.
- Konfigurer frontend med React Router, TypeScript strict, en komponentstruktur, test/lint/build scripts, login/registrer placeholder routes men ingen falsk auth.
- Implementér samme-origin dev-proxy, så `/api` og `/hubs` kan bruges fra frontend, og en enkel landing-shell med design tokens.
- Opret lokal udviklingsopsætning: env examples, valgfri Docker Compose SQL Server med placeholder-adgangskode og README-kommandorække.
- Gennemfør korte smoke-tests af API og browser-shell. Dokumentér nøjagtige startkommandoer i `docs/OPERATIONS.md`.

## Accept – dette trin er først BESTÅET, når

- `dotnet restore` + `dotnet build` lykkes.
- `npm ci` eller korrekt låst install, `npm run build` og lint lykkes.
- API health endpoint og frontend åbner lokalt.
- Ingen fiktive brugere eller hardcoded login i reel app.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_02_DATABASE_OG_DOMAENE.md`**.
