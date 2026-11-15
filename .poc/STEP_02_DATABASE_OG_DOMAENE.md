# STEP 02 – Database, migrationer og invariants

**Kommando til Claude Code:** Læs `CLAUDE.md`, dette dokument og nødvendig eksisterende kode. **Implementér** kun dette trin; kør tilhørende tests; opdatér dokumentationen; giv en kort rapport; **STOP og afvent GO**.

## Mål

Byg den rigtige datamodel og repository/application-lag. Udskyd UI men gør state transitions eksplicitte og sikre.

## Konkrete implementeringsopgaver

- EF Core + SQL Server migrations for IdentityUser, UserProfile, Event, Shift, Assignment, PushSubscription, NotificationOutbox, ReminderDelivery og AuditEntry.
- Notér entydige IDs og timestamps i UTC, local visning i `Europe/Copenhagen`; Shift har start/slut og `Draft/Open/Closed`, capacity > 0, RowVersion.
- Assignment: status aktiv/annulleret, kilde Admin/Self, unik filtreret index for aktive `(ShiftId, UserId)`, audit af ophævelse; undgå hard delete af forretningshistorik.
- Sørg for FK, check constraints, relevante indeks, migrationer og DTO/validatorer. Lav no-tracking read-modeller til sikker filtrering af offentlige og private data.
- Kod domæneinvariants med unit tests, herunder tidsintervaller, kapacitet og max 2 **ark** tivolibilletter.
- Sørg for programmatisk migration eller eksplicit migrationskommando **uden** automatisk uopfordret produktionstilslutning.

## Accept – dette trin er først BESTÅET, når

- Ny tom SQL Server DB kan migreres og læses uden fejl.
- Tests dækker negative valideringer og DB constraints.
- Ingen data fra andre brugeres private skjulte vagter returneres af offentlige forespørgsler.
- Dokumentér schema og migration/rollback.

## Afslutning og disciplin

- Opdatér `docs/STEP_STATUS.md` og mapping til K01–K21, når relevant.
- Bevar allerede fungerende funktionalitet, inklusive sikkerhed og datatransaktioner fra tidligere trin.
- Rapporter hvad der er udført, præcise testkommandoer og PASS/FAIL/IKKE KØRT, evt. blockers.
- Implementér ikke næste trin som 'bonus' og markér det ikke færdigt uden tests.
- Næste trin: **`STEP_03_LOGIN_OG_ROLLER.md`**.
