# Step 02 - Profiler og stamdata

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 01 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Forælderen kan udfylde og rette de stamdata, PDF'en kræver, og administratoren kan se, hvem der har en komplet profil. En komplet profil er forudsætningen for tilmelding (håndhæves i step 07). Krav: K03, K04, K05, K02 (grundlag).

## Omfang
- **Med:**
  - Tabellen `Profiles` (1:1 med `Users`, slettes med brugeren): `FullName`, `Address`, `Phone`, `ShirtSize` (fast liste XS–3XL), `DrinkTickets` (0–50 stk.), `TivoliSheets` (0–2 ark, også som `CHECK`-constraint), `ChildName`, `ChildClass`. E-mail er brugernavnet og står ikke i tabellen.
  - `repositories/Profiles/` med bl.a. `GetByUserIdAsync`, `SaveAsync` (opret eller opdatér) og `GetCompletionByUserIdsAsync`.
  - `ProfileController` (**Min profil**, `/Profile`): vis og gem egen profil. Ingen id i URL'en, altid den indloggede bruger. Layout efter mockup 07 (venstre del). E-mail vises skrivebeskyttet med teksten "Kontakt administratoren for at ændre e-mail".
  - Højre panel på **Min profil**: kun **Skift adgangskode** nu. Push- og e-mailvalg kommer i step 10 og 11, så der er ingen døde knapper.
  - **Brugere** (admin): kolonnen "Profil" (Udfyldt/Mangler) og siden `Users/Profile/{id}` med profilen skrivebeskyttet. Admin kan rette en brugers e-mail (brugernavn) i `Users/Edit`.
  - `HomeController.Index` for brugere: en tydelig besked med link, når profilen mangler.
- **Ikke med:** Server-tjekket ved tilmelding (step 07), notifikationsvalg (step 10/11), sletning og udtræk af persondata (step 15).

## Plan
1. [ ] Model `Profile`, `ProfileConfiguration` (max-længder, unikt `UserId`, `CHECK` på `TivoliSheets` og `DrinkTickets`), `DbSet` og migration `AddProfilesTable`.
2. [ ] `repositories/Profiles/` med Dtos, Interfaces, Mappers og `Documents/Profiles.md`. `IsComplete` beregnes ét sted (mapperen) og bruges af alle.
3. [ ] `ProfileController` + `viewmodels/Profile/` (danske `ErrorMessage` og `[Display]`, `[Range(0, 2)]` på tivoliark) + `views/Profile/Index.cshtml`.
4. [ ] `managers/Members/` (`MemberManager`), fordi brugerlisten og profilvisningen kombinerer `Users` og `Profiles`. `UsersController.Index` og `Profile` bruger manageren.
5. [ ] Udvid `UpdateUserDto`/`UserRepository.UpdateAsync` og `EditUsersViewModel` med e-mail (brugernavn), inkl. tjek af, at brugernavnet ikke er taget.
6. [ ] Menupunktet **Min profil** i layoutet. Beskeden "Udfyld din profil" på forsiden.
7. [ ] Tests: 0/1/2 tivoliark accepteres og 3 afvises (ViewModel og database). Bruger A kan ikke se eller rette bruger B's profil. En bruger uden admin får "Ingen adgang" på `Users/Profile/{id}`. Dubleret e-mail ved rettelse afvises.
8. [ ] Opdatér `web/.claude/CLAUDE.md`: Repositories, Managers, Controllers og endpoints. Under "Bevidste afvigelser": `UsersController` bruger en manager i to actions, og admin kan rette brugernavnet.
9. [ ] Opdatér manualen: afsnittene **Min profil** og **Brugere** (profilstatus, se profil, ret e-mail), skærmbilleder (eller Manglende billeder), ændringslog og `.manuals/README.md`.
10. [ ] Opdatér Status for K03/K04 i `.project/krav/krav.md`.
11. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 02 - Profiler og stamdata" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/data/Models/Profile.cs`, `web/data/Configurations/ProfileConfiguration.cs`, `web/data/WebDbContext.cs`, `web/data/Migrations/` (ny/ændret)
- `web/repositories/Profiles/` (ny), `web/repositories/Users/` (ændret)
- `web/managers/Members/` (ny)
- `web/controllers/ProfileController.cs` (ny), `web/controllers/UsersController.cs`, `web/controllers/HomeController.cs` (ændret)
- `web/viewmodels/Profile/`, `web/views/Profile/` (nye), `web/viewmodels/Users/`, `web/views/Users/`, `web/views/Shared/_Layout.cshtml` (ændret)
- `web/Program.cs` (registreringer)

## Beslutninger
- E-mail = brugernavnet. Det gemmes ikke også i profilen, så der kun er én e-mail at holde opdateret (foreslået af Claude).
- Alle stamdatafelter er påkrævede. Billetantal må gerne være 0 (foreslået af Claude: PDF'en siger "Oprettelsen kræver indtastning af følgende stamdata").
- Trøjestørrelse fra fast liste XS, S, M, L, XL, XXL, 3XL. Væskebilletter 0–50 stk. Klasse som fritekst, højst 20 tegn (foreslået af Claude, kan rettes).

## Test
- `dotnet build` og `dotnet test`.
- Manuelt: log ind som bruger, udfyld **Min profil**, prøv 3 tivoliark (fejlbesked), gem. Log ind som admin, se "Udfyldt" i **Brugere** og åbn profilen.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
