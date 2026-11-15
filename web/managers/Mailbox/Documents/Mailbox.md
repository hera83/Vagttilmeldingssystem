# Mailbox (manager)

**Formål:** Webmailen til systemets egen postkasse (krav K22): læs, svar, skriv, markér og slet. Mails læses direkte fra IMAP og gemmes aldrig i `app.db`.
**Brug det når:** **Indbakke** (`MailboxController`) skal vise eller ændre noget.
**Brug det ikke til:** systemets automatiske mails (det er køen og `NotificationManager`).
**Bruger:** `IMailService` (SMTP og IMAP), `IUserRepository` (genkendelse af brugere, modtagerliste), `MailHtmlSanitizer`, `MailboxStatus`, `TimeProvider`.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetPageAsync(MailFolder, string? search, int page)` | 50 mails pr. side med genkendte brugere (afsender i Indbakke/Papirkurv, modtager i Sendt) og antal ulæste. IMAP-fejl og manglende opsætning kommer tilbage i `Error`, så siden vises med en forklaring. Opdaterer `MailboxStatus` for Indbakke. |
| `ReadAsync(MailFolder, uint uid)` | Mailen med renset HTML (`SafeHtml`), tekst (eller tekst fra HTML'en) og genkendt afsender. Markerer den som læst. |
| `GetReplyDraftAsync(MailFolder, uint uid, string signatureName)` | Modtager = Reply-To eller afsender, emne med "Re:" (ikke dobbelt), hilsen og citat ("> "). |
| `SendReplyAsync(ReplyDto)` | Henter den oprindelige mail igen og sender svaret til dens Reply-To/afsender med `In-Reply-To` og `References`, så det bliver i samme tråd. Formularen kan ikke ændre modtageren. Kopi i Sendt. |
| `SendNewAsync(ComposeDto)` | Ny mail til en bruger eller en fri adresse. Kopi i Sendt. |
| `GetRecipientsAsync()` | Aktive brugere til forslagslisten. |
| `MarkUnreadAsync`, `DeleteAsync`, `GetAttachmentAsync` | Videre til `IMailService`. |

## Faldgruber
- Vis aldrig `HtmlBody` direkte. Brug `SafeHtml` (ingen scripts, events, formularer, iframes, styles eller billeder; links med `target="_blank" rel="noopener noreferrer nofollow"`).
- `MailException` fanges i `GetPageAsync` og ved afsendelse (formularen vises igen med fejlen). Læsning, markering og sletning lader fejlen gå til `GlobalExceptionHandler`.
