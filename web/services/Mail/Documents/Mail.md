# Mail (service)

**Formål:** E-mail fra systemets egen postkasse: invitation, glemt adgangskode, bekræftelse af tilmelding, frigivelser og (step 12) påmindelser.
**Brug det når:** en bruger skal have en mail.
**Brug det ikke til:** at vælge modtagere eller køen (det er `NotificationManager`, `AccountRecoveryManager`, `EnrollmentManager` og repositoriet `Notifications`). Kald aldrig `SendAsync` direkte fra en side: læg mailen i køen, så en SMTP-fejl ikke vælter siden.
**Eksternt system og konfiguration:** SMTP-serveren for systemets postkasse via MailKit 5.0.0 (MIT). Sektion `Mail`: `FromAddress`, `FromName` (standard "Vagttilmelding"), `PickupDirectory` (kun udvikling), `Smtp:Host`, `Smtp:Port` (587/465), `Smtp:Security` (`StartTls`/`SslOnConnect`/`None`), `Smtp:UserName`, `Smtp:Password` (secret). `Mail:Imap:*` (samme felter) er sat op til webmailen, men bruges først i step 18. Mønster: light uden HTTP.

## Metoder
| Metode | Endpoint | Beskrivelse |
|---|---|---|
| `IsConfiguredAsync()` | – | `Mail:FromAddress` og enten `Mail:Smtp:Host` eller `Mail:PickupDirectory` er sat. |
| `SendAsync(SendMailRequestDto request)` | SMTP `MAIL FROM`/`RCPT TO`/`DATA` | Sender én mail (tekst + valgfri HTML) fra `Mail:FromAddress` med headeren `Auto-Submitted: auto-generated`. Med `Mail:PickupDirectory` gemmes den som `.eml`-fil i stedet (relativ sti ud fra content root, fx `app_files/MailPickup`). |

IMAP-metoderne (læs, svar, slet i systemets postkasse) kommer i step 18 (webmail).

## Fejl (`MailException`)
| Situation | Statuskode |
|---|---|
| `Mail:FromAddress`, `Mail:Smtp:Host`, `Mail:Smtp:Password` mangler, eller `Mail:Smtp:Security` er ukendt | 500 (nøglens navn, aldrig værdien) |
| Ugyldig modtageradresse, eller serveren afviser modtageren | 400 (prøves ikke igen) |
| Kan ikke forbinde, TLS fejler | 502 (host, port og nøglenavne) |
| Login afvist | 502 (nævner `Mail:Smtp:UserName`/`Password`) |
| Andre SMTP-fejl | 502 med serverens svar, hvor e-mailadresser er erstattet af `<adresse>` |
| Timeout (30 sek.) | 504 |

## Faldgruber
- Serverens svar kan indeholde e-mailadresser (persondata). De maskeres, før de havner i `LastError` og loggen.
- `Mail:PickupDirectory` må aldrig være sat i drift: så sendes intet. Workeren skriver en advarsel ved opstart, når den er sat.
- Svar fra forældre lander i systemets postkasse (`Mail:FromAddress`), som webmailen (step 18) viser.
