# Members (manager)

**Formål:** Samler brugere (`Users`) og deres profiler (`Profiles`) til administratorens visninger.
**Brug det når:** en side skal vise brugere sammen med profilstatus eller profildata.
**Brug det ikke til:** at ændre brugere eller profiler. Det sker direkte i `IUserRepository`/`IProfileRepository`.
**Bruger:** `IUserRepository`, `IProfileRepository`, `IAssignmentRepository`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetMembersAsync()` | Alle brugere sorteret efter e-mail, hver med `HasCompleteProfile`. |
| `GetMemberAsync(Guid userId)` | Brugeren og profilen (`null`, hvis ikke udfyldt), eller `null`, hvis brugeren ikke findes. |
| `DeleteMemberAsync(Guid userId)` | Sletter brugeren og profilen. `HasAssignments`, hvis brugeren har eller har haft vagter (så skal brugeren deaktiveres i stedet), ellers `Deleted` eller `NotFound`. |

## Faldgruber
- Resultatet indeholder persondata. Kun admin-sider må bruge det.
