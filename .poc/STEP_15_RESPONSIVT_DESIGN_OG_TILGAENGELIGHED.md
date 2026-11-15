# STEP 15 – Enterprise UI på desktop og mobil

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Saml design, formularer og error states til én gennemført app, der føles som samme system på web og PWA.

## Konkrete implementeringsopgaver

- Et ensartet designsystem: tokens, ikoner, knapper, formularfelter, statuschips, tabeller/kort, modal og toast.
- Frontpage, onboarding, auth, profil, ledige vagter, mine vagter, admin-dashboard, events og vagteditor.
- Responsive layouts: desktop admin-sidebar, mobilnavigering, sticky booking-summary, ingen vandret scrolling af kritiske formularer på små skærme.
- Tastatur, labels, modals med focus trap, fokusretur, meddelte validation-fejl, kontrast og ikke-kun-farve status; sigt efter WCAG 2.2 AA.
- Anvend dansk og 24-timers tider i `Europe/Copenhagen`. Vis offline/unauthorized/not-found/empty/reconnect states.
- Gennemgå **alle syv** billeder i `DESIGN_GUIDE.md` og dokumentér afvigelser, når de skyldes egentlige krav eller bedre mobil-UX.
- Kør visuelle tests/screenshots ved Playwright (desktop + mobilviewport).

## Accept – dette trin er først BESTÅET, når

- Centrale brugerflows virker i mindst 360 px mobilbredde og normal desktop.
- Visuelle screenshots oprettes som testartefakter, ikke som hardcodede skærme.
- UI lækker ikke kladder gennem client state eller browse-link.
- Ingen 'døde' menupunkter eller knapper, der foregiver uimplementerede funktioner.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_16_SIKKERHED_PERSONDATA_OG_DRIFTSROBUSTHED.md`**.
