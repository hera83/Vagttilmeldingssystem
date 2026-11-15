# STEP 04 – Fulde profilfelter og validering

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Implementér originalprojektets krævede stamdata, ikke kun email/password. Registrering skal lede til fuldførelse af profil inden booking.

## Konkrete implementeringsopgaver

- Fuldt navn, adresse, telefonnummer, e-mailadresse, trøjestørrelse, antal væskebilletter, antal **tivolibilletark** (0–2), barnets navn samt klasse.
- Udform datakontrakter og servervalidering med klare feltfejl. Max 2 gælder **ark**, ikke styk. Aftal i kode om 'væskebilletter' er et antal (ikke betaling), dokumentér antagelsen.
- Sider: "Min profil", redigér og gem, samt færdiggørelsesflow med åbenlys fejl på manglende stamdata.
- Beskyt privat profil: kun ejer kan læse/redigere egne data; admin får kun nødvendige opslag til tildeling.
- Understøt mobilform og field-level tests. Intet barn eller forælder vises offentligt på signup-lister.
- Bevar konto-e-mail-synkronisering korrekt og gennemfør verificeringsflow, hvis email skiftes.

## Accept – dette trin er først BESTÅET, når

- Test K02–K04 inkl. 0/1/2 accepteret og 3 afvist for tivolibilletark.
- Server afviser booking ved manglende profil (uanset browser UI).
- IDOR-test: bruger A kan ikke læse/opdatere bruger B's profil.
- Design følger `mockups/07_bruger_min_profil.png`.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_05_ADMIN_BEGIVENHEDER.md`**.
