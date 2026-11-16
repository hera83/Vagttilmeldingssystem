# Maintenance (manager)

**Formål:** Dataminimering (GDPR): sletter data, der ikke skal gemmes længere end nødvendigt.
**Brug det når:** noget skal ryddes op med faste mellemrum. Kaldes af `DataCleanupWorker` én gang i døgnet.
**Bruger:** `INotificationRepository`, `IPasswordTokenRepository`, `IConfiguration` (`DataRetention:NotificationDays`), `TimeProvider`.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `CleanupAsync()` | Sletter færdige beskeder (sendt, sprunget over, opgivet), der er ældre end `DataRetention:NotificationDays` (standard 90), og links til adgangskode, der er udløbet eller brugt for mere end et døgn siden. Ventende beskeder slettes aldrig. |

## Faldgruber
- Vagthistorik, brugere og profiler ryddes **ikke** automatisk. Hvor længe de gemmes, skal skolen beslutte (se manualen). Brug **Anonymisér** på brugeren.
- `IConfiguration` læses direkte (afvigelse fra `managers.md` regel 2, se projektfilen), fordi det kun er én tal-indstilling.
