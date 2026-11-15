# Step 15 - Sikkerhed og persondata (gennemgang)

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 14 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Appen behandler oplysninger om forældre og børn. Den skal gennemgås systematisk for adgangsfejl og hærdes, og administratoren skal kunne efterleve GDPR (indsigt og sletning) uden at miste vagthistorikken. Krav: K05, K10, K16 (bekræftes), regel 8 i `krav.md`.

## Omfang
- **Med:**
  - **Gennemgang af alle actions og hubben:** adgang (login/admin/anonym), ejer-scope (ingen id'er, der giver adgang til andres data), anti-forgery på alle POST, mass assignment (ViewModels uden felter som `Status`, `UserId`, `IsAdmin`, hvor de ikke hører til). Resultatet som tabel i projektlaget.
  - **Negative tests** fra `krav.md` samlet i én testklasse, plus: gættede id'er (vagter, tildelinger, profiler), kladder via `Signup/Table` og `MyShifts`, admin-actions som bruger (GET og POST), POST uden token, gentaget tilmelding (replay), SignalR-forbindelse uden login.
  - **Rate limiting** (`AddRateLimiter`, indbygget): login, glemt adgangskode, nulstil adgangskode og tilmelding. Svar 429 med statussiden.
  - **Sikkerheds-headers** (middleware i `infrastructure/Security/`): `Content-Security-Policy` (kun `self`, `connect-src` til SignalR, ingen inline-scripts), `X-Content-Type-Options`, `Referrer-Policy`, `frame-ancestors 'none'`, `Permissions-Policy`. HSTS i Production.
  - **Persondata:**
    - **Anonymisér bruger** (admin): fjerner profil, push-abonnementer og ventende beskeder, sætter brugernavn og visningsnavn til "Slettet bruger <nr>", deaktiverer og bevarer vagthistorikken. Erstatter sletning for brugere med vagter.
    - **Hent data** (admin og brugeren selv): brugerens profil og vagter som JSON eller CSV (GDPR art. 15).
    - Siden **Om dine data** (link fra **Min profil**): hvad der gemmes, hvorfor, hvem der kan se det, og hvordan man får det slettet.
    - Oprydning: sendte beskeder i `Notifications` slettes efter 90 dage, og udløbne `PasswordTokens` slettes (eksisterende worker eller lille vedligeholdelsesjob).
  - **Webmail:** HTML-rensningen testes med kendte XSS-mønstre, eksterne billeder er blokeret, vedhæftninger hentes som download (`Content-Disposition: attachment`) og vises aldrig inline, og kun admin har adgang.
  - **Logs:** gennemgå alle logkald for persondata, e-mails, tokens og push-endpoints.
  - **Pakker:** `dotnet list package --vulnerable --include-transitive` og opdatering, hvis der er fund.
- **Ikke med:** Organisatoriske beslutninger (dataansvarlig, databehandleraftale, opbevaringstid i praksis). De beskrives i manualen som noget, skolen/forældrerådet skal tage stilling til.

## Plan
1. [ ] Endpoint-tabellen i `web/.claude/CLAUDE.md` (alle actions: adgang, scope, anti-forgery) og rettelse af fund.
2. [ ] Negative tests (samlet testklasse).
3. [ ] Rate limiting i `Program.cs` (`AddRateLimiter` + `UseRateLimiter` efter `UseRouting`) og attributter på de relevante actions.
4. [ ] Sikkerheds-headers i `infrastructure/Security/` og test af, at alle sider, SignalR, push og service worker stadig virker med CSP.
5. [ ] Anonymisering og dataudtræk (manager, actions, views) + **Om dine data**.
6. [ ] Oprydning af gamle beskeder og tokens.
7. [ ] Log-gennemgang og pakketjek.
8. [ ] Opdatér `web/.claude/CLAUDE.md`: endpoint-tabel, Infrastruktur, Workers, "Bevidste afvigelser" (rate limiting og headers i `Program.cs`).
9. [ ] Opdatér manualen: **Persondata og GDPR** (anonymisering, udtræk, oprydning, hvad skolen selv skal beslutte), **Sikkerhed** under drift, fejlfinding (429, CSP), ændringslog og `.manuals/README.md`.
10. [ ] Opdatér Status for K05/K10/K16 i `.project/krav/krav.md`.
11. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 15 - Sikkerhed og persondata (gennemgang)" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/Program.cs`, `web/infrastructure/Security/` (ny)
- `web/managers/Members/`, `web/repositories/*`, `web/controllers/UsersController.cs`, `web/controllers/ProfileController.cs`, `web/views/` (ændret/nye)
- `web/bgworkers/` (oprydning)
- `web.Tests/` (negative tests)

## Beslutninger
- Anonymisering frem for sletning, når en bruger har vagter, så vagthistorik og tal bevares (krav-pakken STEP_16, foreslået af Claude).
- Rate limiting i stedet for kontolåsning, så standardens `User`-tabel ikke ændres (foreslået af Claude. Kontolåsning kan tilføjes, hvis brugeren ønsker det).

## Test
- `dotnet build` og `dotnet test` (alle negative tests grønne).
- Manuelt: headers i DevTools, 11 hurtige forkerte logins → 429, anonymisér en testbruger og tjek, at vagthistorikken står som "Slettet bruger".

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Hvor længe skal profiler og vagthistorik gemmes efter et skoleår? Skal besluttes af skolen/forældrerådet. Manualen beskriver mulighederne.
