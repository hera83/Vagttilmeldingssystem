# Enrollment (manager)

**Formål:** Forældrenes egen tilmelding: de åbne vagter med ledige pladser, den samlede tilmelding (multivalg, alt eller intet) og forsidens overblik.
**Brug det når:** en side for brugerne skal vise ledige vagter eller tilmelde brugeren.
**Brug det ikke til:** admin-tildeling (det er `Assignments`-manageren) eller admin-visninger af kladder.
**Bruger:** `IShiftRepository`, `IAssignmentRepository`, `IProfileRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetAvailableAsync(Guid userId)` | Åbne vagter i ikke-arkiverede begivenheder, der ikke er startet, med `Taken`, `Free` og `IsMine`, plus om profilen er komplet. Aldrig kladder. |
| `EnrollAsync(Guid userId, IReadOnlyCollection<Guid> shiftIds)` | `NothingSelected`, `ProfileIncomplete` (serveren håndhæver K02), `Conflict` (med fejl pr. vagt, ingen oprettet) eller `Success`. Går gennem bookingkernen med `OnlyOpen = true` og `Source = Self`. |
| `GetHomeAsync(Guid userId)` | Profilstatus, næste vagt, antal kommende vagter og antal åbne vagter med ledige pladser. |

## Faldgruber
- Fejl kan pege på vagter, brugeren ikke må se (fx en kladdes id i en manipuleret POST). Vis kun titler på vagter, der står i `GetAvailableAsync`. Ellers vises en generel tekst.
- Tallene fra `GetAvailableAsync` er kun til visning. Kun bookingkernen afgør, om der er plads.
