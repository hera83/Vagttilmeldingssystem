# PasswordTokens (repository)

**Formål:** Engangslinks til at vælge adgangskode: invitation af nye brugere (7 dage) og glemt adgangskode (1 time).
**Brug det når:** en bruger skal kunne sætte sin adgangskode via et link i en mail.
**Brug det ikke til:** selve adgangskoden (det er `Users`) eller afsendelsen (det er køen `Notifications`).
**Tabeller/filer:** `PasswordTokens`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `CreateAsync(CreatePasswordTokenDto dto)` | Laver et nyt token (32 tilfældige bytes, base64url) og gemmer kun SHA-256-hashet. Brugerens ubrugte tokens med samme formål slettes i samme transaktion, så kun det nyeste link virker. Returnerer det rå token én gang. |
| `FindAsync(string token)` | Slår op på hashet. Returnerer også brugte og udløbne, så siden kan forklare hvorfor. |
| `ConsumeAsync(Guid id, DateTime nowUtc)` | Markerer tokenet som brugt i én `UPDATE … WHERE UsedAt IS NULL AND ExpiresAt > now`. `true` kun for det første forsøg. |
| `DeleteUnusedForUserAsync(Guid userId)` | Sletter brugerens ubrugte tokens (alle formål), når adgangskoden er sat. |

## Faldgruber
- Det rå token må aldrig logges eller gemmes. Det findes kun i mailen (køens tekst tømmes efter afsendelse).
- Tjek altid formålet (`Purpose`): et nulstillingslink må ikke virke som invitation og omvendt.
