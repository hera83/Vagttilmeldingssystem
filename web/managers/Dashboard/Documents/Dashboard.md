# Dashboard (manager)

**Formål:** Tallene og listerne til admin-dashboardet (`/Dashboard`), bygget på rigtige data. Ingen trendtal, fordi der ikke er historik til dem.
**Brug det når:** dashboardet skal vise noget nyt.
**Brug det ikke til:** deltagerlisten (det er `PlanningManager.GetParticipantsAsync`).
**Bruger:** `IEventRepository`, `IShiftRepository`, `IAssignmentRepository`, `IUserRepository`, `IProfileRepository`, `INotificationRepository`, `IPushService`/`IMailService` (kun om de er sat op), `ReminderWorkerStatus`, `MailboxStatus` (ulæste i systemets postkasse; dashboardet kalder aldrig selv IMAP), `TimeProvider`.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetAsync()` | Aktive begivenheder; åbne vagter, ledige pladser og belægning for kommende vagter (`ShiftOverviewDto`); aktive brugere og hvor mange der mangler profil; de næste 6 begivenheder med vagter, pladser og besatte; begivenheder med kladder; de seneste 8 hændelser (tilmelding, tildeling, fjernelse, frigivelse); køens status for de seneste 7 dage; `ReminderWorkerStatus`; `MailboxStatus` (ulæste); om push og mail er sat op. |

## Faldgruber
- "Seneste aktivitet" viser kun navne på brugere, aldrig adresser, telefon eller børn.
- Kladder tælles aldrig som åbne vagter. "Besatte pladser" tæller alle kommende vagter (også kladder med forhåndstildelinger).
