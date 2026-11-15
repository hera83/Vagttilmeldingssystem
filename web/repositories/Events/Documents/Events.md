# Events (repository)

**Formål:** Begivenheder (fx "Skolefest" eller "Arbejdslørdag") med navn, beskrivelse, sted og en periode i hele datoer.
**Brug det når:** opgaven handler om at oprette, rette, finde eller arkivere begivenheder.
**Brug det ikke til:** vagter (det er `Shifts`) eller frigivelse (sker på vagterne).
**Tabeller/filer:** `Events` (`CHECK`: `EndDate >= StartDate`)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(EventSearchDto search)` | Aktive eller arkiverede begivenheder, evt. filtreret på tekst i navn eller sted. Aktive sorteres med de næste først, arkiverede med de nyeste først. |
| `GetCountsAsync()` | Antal aktive og arkiverede (til fanerne). |
| `GetByIdAsync(Guid id)` | Én begivenhed, eller `null`. Kun til admin: den filtrerer ikke på arkiveret. |
| `CreateAsync(CreateEventDto dto)` | Opretter og returnerer begivenheden. Det udløser ingen frigivelse eller beskeder. |
| `UpdateAsync(Guid id, UpdateEventDto dto)` | Opdaterer, eller `null`. |
| `SetArchivedAsync(Guid id, bool archived)` | Arkiverer eller genåbner. `false`, hvis den ikke findes. |
| `DeleteAsync(Guid id)` | Sletter. `true`, hvis den fandtes. |

## Faldgruber
- Datoerne er `DateOnly` uden tidszone (en hel dag i Danmark). Klokkeslæt hører til vagterne.
- Tekstsøgningen bruger SQLite `LIKE`, som kun ser bort fra store/små bogstaver for a–z, ikke for æ/ø/å.
- Arkiverede begivenheder skal også skjules for brugere, når vagterne vises (step 07).
