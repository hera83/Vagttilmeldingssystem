# Notifications (repository)

**Formål:** Køen af udgående beskeder (push nu, e-mail og påmindelser senere). Én række pr. modtager og kanal. `NotificationWorker` sender dem via `NotificationManager`.
**Brug det når:** en besked skal sendes til en bruger, eller når køens status skal vises.
**Brug det ikke til:** selve afsendelsen (det er servicerne) eller abonnementer (det er `PushSubscriptions`).
**Tabeller/filer:** `Notifications`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `CreateManyAsync(IReadOnlyCollection<CreateNotificationDto> items)` | Lægger beskeder i køen i én transaktion. En `DedupKey`, der findes i forvejen, springes over (idempotent, også efter en genstart). Returnerer antal nye. |
| `GetDueAsync(DateTime nowUtc, int take)` | Ventende beskeder (`Pending`), hvis `NextAttemptAt` er nået, ældste først. |
| `GetByDedupKeyAsync(string dedupKey)` | Én besked ud fra nøglen, eller `null`. |
| `MarkSentAsync(Guid id, DateTime sentAtUtc)` | `Sent` + `SentAt`. |
| `MarkSkippedAsync(Guid id, string reason)` | `Skipped` med årsagen i `LastError` (fx ingen enheder). |
| `ClearContentAsync(Guid id)` | Tømmer `Body` og `Url`. Bruges efter mails med engangslinks (invitation, nulstilling). |
| `GetStatsAsync(DateTime sinceUtc)` | Dashboardet: sendt og påmindelser sendt siden `sinceUtc`, ventende nu, opgivet siden `sinceUtc` og den seneste opgivne besked. |
| `GetForUserAsync(Guid userId, int take)` | Brugerens beskeder, nyeste først (dataudtræk). |
| `DeletePendingForUserAsync(Guid userId)` | Sletter brugerens ventende beskeder (anonymisering). |
| `DeleteFinishedBeforeAsync(DateTime cutoffUtc)` | Sletter færdige beskeder (alt andet end `Pending`), der ikke er ændret siden `cutoffUtc` (oprydning efter `DataRetention:NotificationDays`). |
| `MarkAttemptFailedAsync(Guid id, string error, DateTime? nextAttemptAtUtc)` | Tæller forsøget. Med et tidspunkt prøves igen da (`Pending`), uden opgives beskeden (`Failed`). |

## DedupKey
Formatet bestemmes af den, der lægger beskeden i køen. Nøglen skal være den samme, hver gang den samme hændelse behandles, fx `opened:{eventId}:{openedAt.Ticks}:push:{userId}`. Højst 200 tegn.

## Faldgruber
- Titel og tekst må aldrig indeholde persondata eller titler på kladder (krav K10).
- `LastError` må aldrig indeholde secrets eller abonnementsadresser (kun værtsnavnet).
