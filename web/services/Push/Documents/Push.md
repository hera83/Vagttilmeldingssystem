# Push (service)

**Formål:** Web push (VAPID) til brugernes enheder: Android/Chrome/Edge (FCM), Windows (WNS), Firefox og iPhone/iPad (Apple, iOS 16.4+ og kun som installeret app).
**Brug det når:** en besked skal vises som notifikation på en enhed, også når appen er lukket.
**Brug det ikke til:** at vælge modtagere eller køen (det er `NotificationManager` og repositoriet `Notifications`).
**Eksternt system og konfiguration:** push-tjenesten bag hvert abonnements adresse (`Endpoint`). Biblioteket `Lib.Net.Http.WebPush` 3.3.1 (MIT) står for kryptering (aes128gcm) og VAPID-signaturen. Sektion `Push` (`PublicKey`, `PrivateKey` (secret), `Subject` = `mailto:…` eller `https://…`). Mangler en værdi, er push slået fra. Mønster: light (bibliotek) med navngivet `HttpClient` `"Push"` (timeout 30 sek.).

## Metoder
| Metode | Endpoint | Beskrivelse |
|---|---|---|
| `GetPublicKeyAsync()` | – | Den offentlige nøgle, eller `null`, hvis push ikke er sat op. |
| `SendAsync(SendPushRequestDto request)` | `POST <abonnementets adresse>` | Sender én krypteret besked (JSON `{ title, body, url, tag }`) med TTL og urgency. `Delivered` eller `SubscriptionGone` (404/410). |

## Fejl (`PushException`)
| Situation | Statuskode |
|---|---|
| `Push:*` mangler eller er ugyldig | 500 (nævner nøglenavnene, aldrig værdierne) |
| Adressen er ikke en kendt push-tjeneste (`PushEndpoints`) | 400 (der sendes intet) |
| 401/403 fra push-tjenesten (VAPID afvist) | 502 |
| Andre fejlkoder fra push-tjenesten | samme kode |
| Kan ikke forbinde | 502 |
| Timeout | 504 |

## Hjælpere
- `PushEndpoints.IsAllowed(endpoint)`: kun https på standardport hos `fcm.googleapis.com`, `android.googleapis.com`, `push.services.mozilla.com`, `notify.windows.com` og `push.apple.com` (og deres underdomæner). Beskytter mod, at serveren kan bringes til at kalde vilkårlige adresser (SSRF).
- `VapidKeyGenerator.Generate()`: nyt P-256-nøglepar (base64url). Bruges af `dotnet run --project web -- --generate-vapid-keys`.

## Faldgruber
- Abonnementets adresse og nøgler er hemmeligheder for enheden: log højst værtsnavnet (`PushEndpoints.HostOf`).
- Skiftes VAPID-nøglerne, virker de gamle abonnementer ikke længere. `push.js` tilmelder enheden igen, næste gang brugeren åbner **Min profil**.
- Biblioteket lukker beskedens indhold efter afsendelsen (tests skal læse det i handleren).
