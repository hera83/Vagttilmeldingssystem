# AccountRecovery (manager)

**Formål:** Invitation af nye brugere og glemt adgangskode via engangslinks i mails, så administratoren aldrig kender forældrenes adgangskoder.
**Brug det når:** en bruger skal vælge eller nulstille sin adgangskode via et link.
**Brug det ikke til:** almindelig oprettelse med adgangskode, skift af egen adgangskode eller admins "Ny adgangskode" (det er `IUserRepository` direkte).
**Bruger:** `IUserRepository`, `IPasswordTokenRepository`, `INotificationRepository`, `IMailService` (kun `IsConfiguredAsync`), `AppLinks`, `NotificationSignal`, `TimeProvider`.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `InviteNewUserAsync(InviteUserDto dto)` | Opretter brugeren med en tilfældig, ukendt adgangskode og lægger en invitation i køen (link til `/Account/SetPassword`, gælder 7 dage). `UserNameTaken` eller `MailNotConfigured` (Mail eller `App:Domain` mangler: så oprettes brugeren ikke). |
| `SendInvitationAsync(Guid userId)` | Ny invitation til en eksisterende, aktiv bruger. Tidligere invitationslinks slettes. |
| `RequestPasswordResetAsync(string email)` | Lægger et nulstillingslink i køen (`/Account/ResetPassword`, gælder 1 time), hvis en aktiv bruger har e-mailen. Ellers sker der intet, så svaret ikke afslører, om e-mailen findes. |
| `CheckLinkAsync(string token, PasswordTokenPurpose purpose)` | `Valid`, `Invalid` (findes ikke eller forkert formål), `Expired` eller `Used`. |
| `SetPasswordAsync(string token, PasswordTokenPurpose purpose, string newPassword)` | Bruger linket atomart og sætter adgangskoden via `IUserRepository.SetPasswordAsync` (skifter `SecurityStamp`, så andre sessioner logges ud). Sletter brugerens øvrige ubrugte links. `Valid` = adgangskoden er sat. |

## Faldgruber
- Mailen sendes via køen (`NotificationWorker`). Teksten med linket tømmes, når beskeden er færdigbehandlet.
- Glemt adgangskode må aldrig give et andet svar for ukendte, deaktiverede eller kendte e-mails.
