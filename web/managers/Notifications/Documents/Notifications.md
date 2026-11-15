# Notifications (manager)

**Formål:** Køen af udgående beskeder: hvad der lægges i køen (frigivelser, testbeskeder) og afsendelsen pr. kanal (push nu, e-mail fra step 11). Push-abonnementer for brugerens egne enheder.
**Brug det når:** noget skal have besked uden for siden (push/e-mail), eller når en bruger slår push til/fra.
**Brug det ikke til:** live-opdatering af åbne sider (det er `IShiftUpdatesNotifier` i `infrastructure/Realtime/`).
**Bruger:** `IShiftRepository`, `INotificationRepository`, `IPushSubscriptionRepository`, `IPushService`, `NotificationSignal`, `TimeProvider`.
**Kaldes af:** `NotificationWorker` (kø) og `PushController` (abonnementer og testbesked).

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetPushSettingsAsync()` | Er push sat op, og den offentlige VAPID-nøgle til browseren. |
| `SubscribeAsync(Guid userId, SubscribePushDto dto)` | Gemmer enhedens abonnement. `InvalidEndpoint`, hvis adressen ikke er en kendt push-tjeneste (`PushEndpoints`), `PushDisabled`, hvis push ikke er sat op. |
| `UnsubscribeAsync(Guid userId, string endpoint)` | Sletter brugerens eget abonnement. |
| `QueueTestAsync(Guid userId)` | Lægger en testbesked i køen (højst én pr. minut via `DedupKey`) og vækker workeren. `NoDevices`/`PushDisabled`. |
| `QueueOpenedShiftsAsync()` | Finder frigivelser uden beskeder (`Shifts.OpenNotifiedAt = null`), lægger én generisk besked pr. aktiv bruger med abonnement i køen og markerer vagterne. Frigivelser ældre end 24 timer, eller hvor ingen vagter længere kan tilmeldes, markeres uden beskeder. |
| `SendDueAsync()` | Sender op til `BatchSize` (100) ventende beskeder. Push: til alle brugerens enheder; udløbne abonnementer slettes; fejl giver op til 3 forsøg (efter 1 og 5 minutter), derefter `Failed`. |

## Regler
- Beskedtekster er generiske: begivenhedens navn og antal åbne vagter. Aldrig persondata og aldrig kladder (krav K10).
- `DedupKey` for frigivelser: `opened:{eventId:N}:{openedAt.Ticks}:push:{userId:N}`. Vagterne markeres **efter** køen er skrevet: går noget galt imellem, behandles frigivelsen igen, og nøglen forhindrer dobbelte beskeder.
- `PushException` fanges pr. abonnement i `SendDueAsync` (bevidst afvigelse fra `services.md` regel 14), så fejlen registreres på beskeden, og køen fortsætter.

## Faldgruber
- Tests kalder `QueueOpenedShiftsAsync`/`SendDueAsync` direkte med `Notifications:WorkerEnabled = false`. Kør aldrig workeren og direkte kald samtidig mod samme database, ellers kan en besked sendes to gange.
- E-mail-kanalen (step 11) skal have sin egen gren i `SendDueAsync` og sine egne beskeder i køen (`Channel = Email`).
