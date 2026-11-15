# Shifts (repository)

**Formål:** Vagter under en begivenhed: titel, beskrivelse, tidsrum (UTC), maksimalt antal personer og status (`Draft` = skjult kladde, `Open` = åben for tilmelding).
**Brug det når:** opgaven handler om at oprette, rette, slette eller vise vagter.
**Brug det ikke til:** tildelinger og tilmeldinger (det er `Assignments`).
**Tabeller/filer:** `Shifts` (FK til `Events` med `Restrict`, `CHECK` kapacitet 1–500 og slut > start)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetByIdAsync(Guid id)` | Én vagt i enhver status, eller `null`. **Kun til admin.** |
| `GetByEventIdAsync(Guid eventId, ShiftStatus? status)` | Begivenhedens vagter sorteret efter starttid, evt. kun én status. **Kun til admin.** |
| `AnyForEventAsync(Guid eventId)` | Har begivenheden vagter? Bruges til at spærre sletning af begivenheden. |
| `CreateAsync(Guid eventId, CreateShiftDto dto)` | Opretter en vagt. Status er **altid** `Draft`. Dto'en har ikke engang et statusfelt. |
| `UpdateAsync(Guid id, UpdateShiftDto dto)` | Retter titel, beskrivelse, tid og kapacitet. Status ændres aldrig her. `null`, hvis den ikke findes. |
| `DeleteAsync(Guid id)` | Sletter. `true`, hvis den fandtes. |

## Faldgruber
- **Kladder må aldrig nå brugerne (krav K10).** Alle forespørgsler, der viser vagter til brugere, skal filtrere på `Status == Open` i selve forespørgslen. Metoderne ovenfor er kun til admin.
- Tidspunkter er UTC. SQLite giver dem tilbage med `DateTimeKind.Unspecified`, så brug `DateTimeExtensions` til visning (den sætter `Kind` selv).
- Kapacitet og tid valideres også af databasen. Et forkert tal giver en `DbUpdateException`.
