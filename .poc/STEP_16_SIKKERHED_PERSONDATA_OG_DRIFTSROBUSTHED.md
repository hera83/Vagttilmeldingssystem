# STEP 16 – Sikkerhed og GDPR-hygiejne

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Gennemfør systematisk sikkerhedsgennemgang, fordi applikationen håndterer forældre- og børneoplysninger.

## Konkrete implementeringsopgaver

- Gennemgå samtlige endpoints for auth, rollespærrer, owner-scope, mass assignment, IDOR, SQL injection, XSS, request rate limits og CSRF.
- Beskyt push subscriptions og evt. mail-infrastruktur. Aflist kun egne tildelinger; admins opslag skal være formålsbegrænset.
- Dataminimering: de i PDF'en nævnte felter og nødvendige revisionsoplysninger; ingen CPR eller betalingsoplysninger.
- Indfør GDPR-venlige slette-/eksportprocedurer og opbevaringspolitik, uden at slette vagthistorik ukontrolleret. Vær tydelig om databehandleransvar og nødvendige organisatoriske beslutninger, som kode ikke løser.
- Sikre headers: HTTPS/HSTS, CSP efter konkret frontendkrav, cookie policies, CORS begrænset, secure logs og secrets externalized.
- Sårbarhedsscanning af npm/NuGet, dependency-låsning, sikker fejloutput og audit på admin-handlinger.
- Gennemfør negativ pen-testliste, herunder hidden event enumeration, admin escalation, callback/websocket leak, fake capacity, replay booking.

## Accept – dette trin er først BESTÅET, når

- Alle dokumenterede IDOR/rolle-/CSRF-tests passerer.
- Secrets er ikke i kode, docs, logs eller Git.
- Databehandling/retention og log-redaktion er dokumenteret.
- `dotnet` og npm sikkerhedstjek er udført eller blocker forklaret.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_17_AUTOMATISKE_TESTS.md`**.
