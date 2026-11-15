# Mail (service)

**Formål:** E-mail fra systemets egen postkasse: invitation, glemt adgangskode, bekræftelse af tilmelding, frigivelser og (step 12) påmindelser.
**Brug det når:** en bruger skal have en mail.
**Brug det ikke til:** at vælge modtagere eller køen (det er `NotificationManager`, `AccountRecoveryManager`, `EnrollmentManager` og repositoriet `Notifications`). Kald aldrig `SendAsync` direkte fra en side: læg mailen i køen, så en SMTP-fejl ikke vælter siden.
**Eksternt system og konfiguration:** SMTP-serveren for systemets postkasse via MailKit 5.0.0 (MIT). Sektion `Mail`: `FromAddress`, `FromName` (standard "Vagttilmelding"), `PickupDirectory` (kun udvikling), `Smtp:Host`, `Smtp:Port` (587/465), `Smtp:Security` (`StartTls`/`SslOnConnect`/`None`), `Smtp:UserName`, `Smtp:Password` (secret). `Mail:Imap:Host`, `Port` (993/143), `Security` (standard `SslOnConnect`), `UserName`, `Password` (secret), `SaveSentCopy` (standard `true`), `SentFolder`/`TrashFolder` (standard `Sent`/`Trash`, bruges kun uden special-use), `PollIntervalSeconds` (300, `MailboxPollWorker`). Mønster: light uden HTTP; IMAP-metoderne ligger i `MailService.Imap.cs` (partial).

## Metoder
| Metode | Endpoint | Beskrivelse |
|---|---|---|
| `IsConfiguredAsync()` | – | `Mail:FromAddress` og enten `Mail:Smtp:Host` eller `Mail:PickupDirectory` er sat. |
| `SendAsync(SendMailRequestDto request)` | SMTP `MAIL FROM`/`RCPT TO`/`DATA` | Sender én mail (tekst + valgfri HTML) fra `Mail:FromAddress` med headeren `Auto-Submitted: auto-generated`. Med `Mail:PickupDirectory` gemmes den som `.eml`-fil i stedet (relativ sti ud fra content root, fx `app_files/MailPickup`). |

| `SendAsync` med `InReplyTo`/`References` | | Svar bliver i samme tråd hos modtageren. Med `SaveCopyToSent` (kun webmailen) gemmes en kopi i Sendt via IMAP `APPEND`, når mailen er sendt med SMTP (ikke med pickup-mappen). Fejler kopien, står det i `SentCopyError`, men mailen er sendt. |
| `IsMailboxConfiguredAsync()` | – | `Mail:Imap:Host` er sat. |
| `GetMailboxStatusAsync()` | IMAP `STATUS INBOX (UNSEEN)` | Antal ulæste i Indbakke. |
| `ListMessagesAsync(ListMessagesRequestDto)` | IMAP `SEARCH` + `FETCH` | Mails i Indbakke, Sendt eller Papirkurv, nyeste først, søgning i afsender og emne, sider (højst 200), antal ulæste. En Sendt-/Papirkurv-mappe, der ikke findes endnu, giver en tom liste. |
| `GetMessageAsync(MailFolder, uint uid)` | IMAP `FETCH BODY.PEEK[]` | Én mail med tekst, rå HTML (skal renses før visning), Reply-To, Message-ID, References og vedhæftninger. Markerer den ikke som læst. `null`, hvis den ikke findes. |
| `SetSeenAsync(MailFolder, uint uid, bool seen)` | IMAP `STORE ±FLAGS \Seen` | Læst/ulæst. |
| `DeleteAsync(MailFolder, uint uid)` | IMAP `MOVE` / `STORE \Deleted` + `EXPUNGE` | Flytter til Papirkurv (oprettes, hvis den mangler), eller sletter helt fra Papirkurv. |
| `GetAttachmentAsync(MailFolder, uint uid, int index)` | IMAP `FETCH` | Én vedhæftning (højst 25 MB, ellers 413). |

**Mapper:** Sendt og Papirkurv findes via IMAP special-use (`\Sent`, `\Trash`). Ellers på navnet (`Mail:Imap:SentFolder`/`TrashFolder`) i den personlige rod eller under INBOX. Findes de ikke, oprettes de første gang der gemmes en kopi eller slettes (fx `INBOX.Sent` og `INBOX.Trash` hos one.com/Dovecot).

## Fejl (`MailException`)
| Situation | Statuskode |
|---|---|
| `Mail:FromAddress`, `Mail:Smtp:Host`, `Mail:Smtp:Password` mangler, eller `Mail:Smtp:Security` er ukendt | 500 (nøglens navn, aldrig værdien) |
| Ugyldig modtageradresse, eller serveren afviser modtageren | 400 (prøves ikke igen) |
| Kan ikke forbinde, TLS fejler | 502 (host, port og nøglenavne) |
| Login afvist | 502 (nævner `Mail:Smtp:UserName`/`Password`) |
| Andre SMTP-fejl | 502 med serverens svar, hvor e-mailadresser er erstattet af `<adresse>` |
| Timeout (30 sek.) | 504 |
| IMAP: `Mail:Imap:Host`/`UserName`/`Password` mangler eller `Security` er ukendt | 500 (nøglernes navne) |
| IMAP: kan ikke forbinde, login afvist, protokolfejl | 502 (host, port og nøglenavne; serverens svar med maskerede adresser) |
| IMAP: mappen findes ikke | 404 |

## Faldgruber
- Serverens svar kan indeholde e-mailadresser (persondata). De maskeres, før de havner i `LastError` og loggen.
- `Mail:PickupDirectory` må aldrig være sat i drift: så sendes intet. Workeren skriver en advarsel ved opstart, når den er sat.
- Svar fra forældre lander i systemets postkasse (`Mail:FromAddress`), som webmailen viser.
- Én IMAP-forbindelse pr. kald (login hver gang). Det er nok til én administrator ad gangen; dashboardet kalder aldrig IMAP selv (`MailboxStatus`).
- Postkassen kan indeholde andre mails end systemets. Webmailen viser alt i mapperne; slet aldrig noget automatisk.
