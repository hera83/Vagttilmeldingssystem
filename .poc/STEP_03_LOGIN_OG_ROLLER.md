# STEP 03 – Registrering, login og adgangskontrol

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Lav reel authentication og authorization, før admin- og bruger-API etableres.

## Konkrete implementeringsopgaver

- ASP.NET Core Identity med registrering, login, logout, current-user og sikker auth-cookie på samme origin; brug anti-CSRF beskyttelse ved mutationer.
- Roller: `Administrator`, `Bruger`. Standardregistrering er altid Bruger; selvvalgt Admin-rolle er umulig.
- Admin kan provisions via eksplicit lokal CLI-/invitationflow; i udvikling må en seed kun virke med eksplicit flag og unik adgangskode fra env, aldrig konstant kode.
- Rate limit login/registrering; låsning, password policy, læsbar feedback, sikre fejl.
- React: login/registrering, route guards og auth-refresh uden at gemme adgangstokens i localStorage.
- Policies på admin-API og ejerne af egne data; log kun nødvendige audit-events.
- Integrationstest for 401/403, CSRF og eskalering ved manipuleret body.

## Accept – dette trin er først BESTÅET, når

- Bruger kan oprette konto, logge ind, ud og beholde session sikkert.
- En normal bruger får 403 på admin-endpoints og kan ikke registrere sig som admin.
- Manglende/ugyldig CSRF afvises på relevante mutationer.
- Ingen demo-adgangskoder er med i produktionsopsætningen.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_04_PROFIL_OG_BRUGEROPRETTELSE.md`**.
