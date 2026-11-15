# Assignments (manager)

**Formål:** Administratorens tildeling af vagter (også skjulte kladder, krav K11) og fjernelse af brugere fra vagter (krav K17).
**Brug det når:** en admin-side skal tildele, fjerne eller vise, hvem der er på en vagt.
**Brug det ikke til:** brugerens egen tilmelding (det er `Enrollment`, step 07) eller visning af brugerens egne vagter (brug `IAssignmentRepository` direkte).
**Bruger:** `IShiftRepository`, `IEventRepository`, `IUserRepository`, `IAssignmentRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetAssignOverviewAsync(Guid shiftId, string? search)` | Vagt, begivenhed, de aktive tildelinger og op til 50 aktive brugere, der ikke er på vagten, filtreret på navn/e-mail. `null`, hvis vagten ikke findes. |
| `AssignAsync(Guid shiftId, Guid userId, Guid assignedByUserId)` | Tildeler via bookingkernen (`OnlyOpen = false`, `Source = Admin`). `Success`, `ShiftNotFound`, `UserNotFound`, `UserInactive`, `AlreadyAssigned` eller `Full`. |
| `UnassignAsync(Guid shiftId, Guid assignmentId, Guid removedByUserId)` | Afmelder (soft cancel). `NotFound`, hvis tildelingen ikke hører til vagten eller allerede er fjernet. |

## Faldgruber
- Admin-tildelinger tæller med i kapaciteten. Admin kan ikke overbooke.
- Der findes bevidst ingen metode, hvor en bruger selv kan afmelde, bytte eller give en vagt videre (krav K16).
