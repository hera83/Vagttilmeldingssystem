# Profiles (repository)

**Formål:** Forældrenes stamdata fra PDF'en: fulde navn, adresse, telefon, trøjestørrelse, væskebilletter, tivolibilletter (ark), barnets navn og klasse. 1:1 med `Users`.
**Brug det når:** opgaven skal læse eller gemme en brugers stamdata, eller vide om profilen er komplet (forudsætning for tilmelding, krav K02).
**Brug det ikke til:** login, e-mail eller roller (det er `Users`). E-mailen er brugernavnet og står ikke i profilen.
**Tabeller/filer:** `Profiles` (slettes sammen med brugeren, cascade)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetByUserIdAsync(Guid userId)` | Profilen, eller `null`, hvis brugeren ikke har udfyldt den endnu. |
| `IsCompleteAsync(Guid userId)` | `true`, hvis profilen findes og alle påkrævede felter er udfyldt. Bruges til at spærre tilmelding. |
| `GetCompleteUserIdsAsync()` | Id'er på alle brugere med komplet profil (til lister). |
| `SaveAsync(Guid userId, SaveProfileDto dto)` | Opretter eller opdaterer brugerens profil og returnerer den. |

## Faldgruber
- "Komplet" afgøres ét sted: `ProfileMapper.IsCompleteExpression` (og samme betingelse i `ToDtoExpression`). Ret begge, hvis felterne ændres.
- Databasen har `CHECK` på `TivoliSheets` (0–2) og `DrinkTickets` (0–50). Et forkert tal giver en `DbUpdateException`, så validér i ViewModel'en først.
- `ShirtSize` skal være en af værdierne i `ShirtSizes.All`.
- Persondata: profiler må kun vises for ejeren og for administratorer.
