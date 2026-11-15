# Assignments (repository)

**Formål:** Tildelinger (en brugers plads på en vagt) og **bookingkernen**, der sikrer først-til-mølle uden overbooking (krav K13) og alt-eller-intet ved flere vagter (krav K14).
**Brug det når:** nogen skal på en vagt (admin-tildeling eller brugerens egen tilmelding), fjernes fra en vagt, eller når en brugers egne vagter skal vises.
**Brug det ikke til:** at oprette eller ændre vagter (det er `Shifts`).
**Tabeller/filer:** `Assignments` (læser også `Shifts`, `Events` og `Users` i forespørgslerne)

## Metoder
| Metode | Beskrivelse |
|---|---|
| `CreateManyAsync(CreateAssignmentsDto dto)` | Bookingkernen: tildeler brugeren alle vagterne i én transaktion, eller ingen. Fejl pr. vagt: `NotFound`, `NotOpen` (kun med `OnlyOpen`), `AlreadyAssigned`, `Full`. |
| `CancelAsync(Guid assignmentId, Guid cancelledByUserId)` | Afmelder (soft cancel: `IsActive = false`, `CancelledAt`, `CancelledByUserId`). Pladsen er ledig igen med det samme. `null`, hvis den ikke findes eller allerede er afmeldt. **Kun admin må kalde den.** |
| `GetByIdAsync(Guid assignmentId)` | Én tildeling (også afmeldte), eller `null`. |
| `GetActiveByShiftAsync(Guid shiftId)` | Aktive tildelinger på en vagt med brugerens navn og e-mail. **Kun til admin.** |
| `GetActiveCountsAsync(IEnumerable<Guid> shiftIds)` | Antal aktive tildelinger pr. vagt (vagter uden tildelinger er ikke med). |
| `GetActiveForUserAsync(Guid userId)` | Brugerens egne aktive vagter med begivenhedens navn og sted, sorteret efter starttid. Indeholder også tildelte kladder. |
| `GetActiveForUserAndShiftAsync(Guid userId, Guid shiftId)` | Én af brugerens egne vagter, eller `null` (også hvis vagten findes, men ikke er brugerens). |
| `AnyForUserAsync(Guid userId)` | Har brugeren tildelinger (også afmeldte)? Så kan brugeren ikke slettes. |

## Transaktionsgrænse (vigtigt)
`CreateManyAsync` gør alt i én transaktion:
1. `BeginTransactionAsync()`. Microsoft.Data.Sqlite starter med `BEGIN IMMEDIATE`, så transaktionen tager skrivelåsen med det samme. Andre skrivere venter (busy timeout, standard 30 sek.) i stedet for at blive blandet ind.
2. Læs vagterne (status, kapacitet, arkiveret begivenhed), antal aktive tildelinger og brugerens egne aktive tildelinger.
3. Er der én fejl, rulles alt tilbage. Ellers indsættes alle tildelinger, og der committes.

Det delvist unikke index (`ShiftId`, `UserId`) `WHERE IsActive = 1` er sikkerhedsnettet mod dobbelte tildelinger. Der er ingen `lock` i C#, så det virker også, hvis flere processer deler databasefilen. Testene `AssignmentConcurrencyTests` kører 50 samtidige forsøg mod en rigtig SQLite-fil.

## Faldgruber
- Kapaciteten tælles **kun** i transaktionen. Brug aldrig et tal, der er læst før, til at afgøre en tilmelding.
- `MyShiftDto` indeholder kun brugerens egne vagter. Byg aldrig en visning, der viser andre vagter fra samme begivenhed til en bruger, før de er åbne.
- Rækker slettes aldrig ved afmelding (historik). De slettes kun, hvis selve vagten slettes (cascade). Det er tilladt, når vagten ingen aktive tildelinger har.
