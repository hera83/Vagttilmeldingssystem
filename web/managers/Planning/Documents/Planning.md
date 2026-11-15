# Planning (manager)

**Formål:** Administratorens planlægning: begivenheder med deres vagter og reglerne, der går på tværs af dem (vagten skal ligge i begivenhedens periode, en begivenhed med vagter kan ikke slettes).
**Brug det når:** en admin-side skal vise eller ændre vagter i sammenhæng med begivenheden.
**Brug det ikke til:** visning for forældre (kladder må aldrig nå dem) eller tildelinger.
**Bruger:** `IEventRepository`, `IShiftRepository`, `IAssignmentRepository`, `IProfileRepository` (deltagerlisten), `IShiftUpdatesNotifier`, `NotificationSignal`

## Metoder
| Metode | Beskrivelse |
|---|---|
| `GetEventAsync(Guid eventId)` | Begivenheden, eller `null`. |
| `GetParticipantsAsync(Guid eventId)` | Deltagerlisten: aktive tildelinger i begivenheden med profilens stamdata (navn, telefon, e-mail, trøjestørrelse, billetter, barn og klasse), sorteret efter vagtens start. `null`, hvis begivenheden ikke findes. **Kun admin** (persondata). |
| `SearchEventsAsync(EventSearchDto search)` | Begivenhederne fra søgningen med antal kladder og åbne vagter pr. begivenhed. |
| `OpenShiftsAsync(Guid eventId, IReadOnlyCollection<Guid> shiftIds, Guid openedByUserId)` | **Frigivelsen.** `EventNotFound`, `EventArchived` eller `Success` med de vagter, der blev åbnet nu. Tom liste, hvis de allerede var åbne. Forhåndstildelinger bevares, og kun de resterende pladser bliver ledige. |
| `GetEventOverviewAsync(Guid eventId, ShiftStatus? statusFilter)` | Begivenheden med vagterne (evt. kun én status), antal kladder/åbne og antal besatte pladser pr. vagt. `null`, hvis den ikke findes. |
| `GetShiftEditorAsync(Guid shiftId)` | Vagten og dens begivenhed (til ret-formularen). |
| `CreateShiftAsync(Guid eventId, CreateShiftDto dto)` | `Success`, `EventNotFound` eller `OutsideEventPeriod`. Vagten bliver altid en kladde. |
| `UpdateShiftAsync(Guid shiftId, UpdateShiftDto dto)` | `Success`, `ShiftNotFound`, `OutsideEventPeriod` eller `CapacityBelowTaken`. |
| `DeleteShiftAsync(Guid shiftId)` | `Success`, `ShiftNotFound` eller `ShiftHasAssignments`. |
| `DeleteEventAsync(Guid eventId)` | `Success`, `EventNotFound` eller `EventHasShifts` (så skal den arkiveres i stedet). |

## Faldgruber
- Periodetjekket bruger vagtens startdato i dansk tid. En vagt må gerne slutte efter midnat på sidste dag.
- Alt her viser kladder. Kun admin-sider må bruge manageren.
