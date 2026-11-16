# Members (manager)

**Formål:** Samler brugere (`Users`) og deres profiler (`Profiles`) til administratorens visninger.
**Brug det når:** en side skal vise brugere sammen med profilstatus eller profildata.
**Brug det ikke til:** at ændre brugere eller profiler. Det sker direkte i `IUserRepository`/`IProfileRepository`.
**Bruger:** `IUserRepository`, `IProfileRepository`, `IAssignmentRepository`, `IPushSubscriptionRepository`, `INotificationRepository`, `IPasswordTokenRepository` (anonymisering og dataudtræk), `TimeProvider`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetMembersAsync()` | Alle brugere sorteret efter e-mail, hver med `HasCompleteProfile`. |
| `GetMemberAsync(Guid userId)` | Brugeren og profilen (`null`, hvis ikke udfyldt), eller `null`, hvis brugeren ikke findes. |
| `AnonymizeMemberAsync(Guid userId)` | GDPR: sletter profil, push-abonnementer, ventende beskeder og ubrugte links, og anonymiserer brugeren ("Slettet bruger …", ingen login). Vagthistorikken bevares. Brugeren anonymiseres som sidste trin, så handlingen kan køres igen. |
| `ExportMemberAsync(Guid userId)` | Dataudtræk (GDPR art. 15): bruger, profil, e-mail-valg, alle tildelinger (også afmeldte), enheder med push (kun push-tjenestens værtsnavn) og de seneste 500 beskeder. Aldrig adgangskode-hash, security stamp eller push-nøgler. |
| `DeleteMemberAsync(Guid userId)` | Sletter brugeren og profilen. `HasAssignments`, hvis brugeren har eller har haft vagter (så skal brugeren deaktiveres i stedet), ellers `Deleted` eller `NotFound`. |

## Faldgruber
- Resultatet indeholder persondata. Kun admin-sider må bruge det.
