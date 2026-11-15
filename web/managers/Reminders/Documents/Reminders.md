# Reminders (manager)

**Formål:** Påmindelser 48 og 24 timer før vagtstart via push og e-mail (krav K19, K20). Kaldes af `ReminderWorker` (hver `Reminders:WorkerIntervalSeconds`, standard én gang i timen).
**Brug det når:** påmindelsesreglerne eller teksterne skal ændres.
**Brug det ikke til:** selve afsendelsen (det er køen og `NotificationManager`).
**Bruger:** `IAssignmentRepository`, `INotificationRepository`, `IPushSubscriptionRepository`, `IProfileRepository`, `IPushService` og `IMailService` (kun om de er sat op), `AppLinks`, `NotificationSignal`, `TimeProvider`.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `QueueDueAsync()` | Finder aktive tildelinger hos aktive brugere med start inden for 48 timer og lægger de påmindelser i køen, der er "due" nu. Returnerer antal tildelinger og nye beskeder. Vækker `NotificationWorker`. |
| `DueKind(UpcomingAssignmentDto, DateTime nowUtc)` (statisk) | Reglerne nedenfor. Testes direkte. |

## Regler
- **48 t (`Reminder48`):** når `start − 48 t ≤ nu < start − 24 t`, og kun hvis tildelingen fandtes ved `start − 48 t`. En tildeling oprettet senere (fx 30 t før) får kun 24-timers påmindelsen, så "om 2 dage" aldrig kommer lige efter tilmeldingen.
- **24 t (`Reminder24`):** når `start − 24 t ≤ nu < start`, også for tildelinger oprettet mindre end 24 t før.
- Startede vagter, afmeldte tildelinger og deaktiverede brugere får ingen. Tildelte kladder får påmindelser (brugeren ser dem i sin kalender).
- Kanaler: push, hvis push er sat op, og brugeren har et abonnement; e-mail, hvis mail er sat op, og brugeren ikke har slået e-mail-beskeder fra.
- `DedupKey = reminder:{48|24}:{assignmentId:N}:{StartUtc.Ticks}:{push|email}`. Køens unikke index gør gentagne og parallelle scanninger ufarlige, også efter en genstart. Flyttes vagtens starttid, giver den nye starttid nye påmindelser.
- Beskeden gemmer `AssignmentId`. `NotificationManager` springer den over (`Skipped`) ved afsendelse, hvis tildelingen er afmeldt, eller vagten er startet.
- Tekster (dansk tid, også hen over sommertid): "Du har en vagt om 2 dage: man. 29. mar. 2027 kl. 10:00." og "Husk din vagt i morgen kl. 10:00." ("i dag"/"i morgen"/"om N dage" ud fra datoerne). Push: titel "Påmindelse: {vagt}" + første linje + begivenhed og sted. Mail: emne "Påmindelse: du har en vagt om 2 dage" / "Påmindelse: husk din vagt i morgen", vagtens detaljer og link til `/MyShifts/Details/{shiftId}`.

## Faldgruber
- Påmindelsen sendes ved den første scanning efter tidspunktet, altså op til ét interval (standard 60 min.) senere.
- `NotBeforeUtc` er scanningens tidspunkt. Tests, der styrer uret med `FakeTimeProvider`, skal også sende køen med samme ur.
