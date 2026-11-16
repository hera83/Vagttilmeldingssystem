# PushSubscriptions (repository)

**Formål:** Enhedernes web push-abonnementer. Et abonnement er brugerens samtykke til push på den enhed (krav K18).
**Brug det når:** en bruger slår push til eller fra på en enhed, eller når beskeder skal sendes til en brugers enheder.
**Brug det ikke til:** selve afsendelsen (det er servicen `Push`) eller køen (det er `Notifications`).
**Tabeller/filer:** `PushSubscriptions` (læser også `Users` for at finde aktive brugere)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SaveAsync(SavePushSubscriptionDto dto)` | Opretter abonnementet, eller opdaterer nøglerne og ejeren, hvis adressen (`Endpoint`) findes. Logger en anden bruger ind på samme enhed og slår push til, flyttes abonnementet til den bruger. |
| `DeleteForUserAsync(Guid userId, string endpoint)` | Sletter brugerens **eget** abonnement. `false`, hvis det ikke findes eller tilhører en anden. |
| `DeleteAsync(Guid id)` | Sletter et abonnement, som push-tjenesten har meldt udløbet (404/410). |
| `DeleteAllForUserAsync(Guid userId)` | Sletter alle brugerens abonnementer (anonymisering). |
| `GetByUserIdAsync(Guid userId)` | Brugerens abonnementer (én pr. enhed). |
| `GetActiveSubscriberIdsAsync()` | Id'er på aktive brugere med mindst ét abonnement. |
| `MarkSuccessAsync(Guid id, DateTime atUtc)` | Sætter `LastSuccessAt` efter en vellykket levering. |

## Faldgruber
- `Endpoint`, `P256dh` og `Auth` er hemmeligheder for enheden: vis dem aldrig, og log dem aldrig (log højst værtsnavnet).
- Adressen valideres mod de kendte push-tjenester (`PushEndpoints`) i `NotificationManager`, før den gemmes. Repositoriet gemmer, hvad det får.
