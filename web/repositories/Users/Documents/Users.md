# Users (repository)

**Formål:** Brugere og login: oprettelse, adgangskoder (hash med `PasswordHasher<User>`), roller (admin) og aktiv/deaktiveret.
**Brug det når:** opgaven handler om brugerkonti, login eller adgangskoder i appens egen database.
**Brug det ikke til:** forældrenes stamdata (det er `Profiles`) eller data fra eksterne systemer.
**Tabeller/filer:** `Users`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `AnyAsync()` | Findes der brugere? Bruges til første administrator ved opstart. |
| `GetAllAsync()` | Alle brugere sorteret efter brugernavn. |
| `GetByIdAsync(Guid id)` | Én bruger, eller `null`. Bruges også af `UserCookieEvents` ved hvert request. |
| `ValidateCredentialsAsync(string userName, string password)` | Aktiv bruger med korrekt adgangskode, ellers `null`. Sætter `LastLoginAt` og opgraderer hashet, hvis `PasswordHasher` beder om det. |
| `CreateAsync(CreateUserDto dto)` | Opretter og returnerer brugeren, eller `null`, hvis brugernavnet findes. |
| `UpdateAsync(Guid id, UpdateUserDto dto)` | Opdaterer, eller `null`. Skifter `SecurityStamp`, hvis `IsActive` ændres. |
| `SetPasswordAsync(Guid id, string newPassword)` | Admin sætter ny adgangskode. Skifter `SecurityStamp`. `false`, hvis brugeren ikke findes. |
| `ChangePasswordAsync(Guid id, string currentPassword, string newPassword)` | Brugerens egen ændring. `null`, hvis den nuværende adgangskode er forkert. Skifter `SecurityStamp`. |
| `DeleteAsync(Guid id)` | `true`, hvis rækken fandtes. |

## Faldgruber
- Brugernavnet er brugerens e-mailadresse (projektets regel). Kolonnen har collation `NOCASE`, så sammenligning er uden forskel på store og små bogstaver.
- `PasswordHash` forlader aldrig repositoriet. `UserDto` har det ikke.
- Ændring af adgangskode eller aktiv-status skifter `SecurityStamp`, så andre sessioner logges ud ved næste request.
