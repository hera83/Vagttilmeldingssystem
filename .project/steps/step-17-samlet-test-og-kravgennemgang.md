# Step 17 - Samlet test og kravgennemgang

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | web |
| Afhænger af | Step 16 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Bevise, at hele systemet virker som en realistisk skolefest fra en tom database, og gå K01–K21 igennem ét for ét. Systemet kaldes først "færdigt", når alle krav er godkendt eller ærligt står som åbne med en årsag. Krav: K01–K21.

## Omfang
- **Med:**
  - Fuld automatisk kørsel: `dotnet build`, `dotnet test` (alle unit-, integrations-, samtidigheds- og negative tests), `dotnet list package --vulnerable`.
  - **Scenarie "Skolefest"** på en tom database (slet `app_dbs/`), gennemført i UI'et:
    1. Admin opretter tre forældre (A, B, C) med invitation. De sætter adgangskode via mailen og udfylder profilen.
    2. Admin opretter "Skolefest" med 6–8 vagter (alle kladder) og tildeler én kladdevagt til A.
    3. A ser vagten under **Mine vagter** ("Tildelt af administrator"). B og C ser ingenting, heller ikke via URL.
    4. Admin åbner for tilmelding med bekræftelse. B og C har **Ledige vagter** åben på desktop og telefon → vagterne dukker op live. Push og e-mail modtages.
    5. B vælger tre vagter og bekræfter → **Mine vagter** + bekræftelsesmail.
    6. B og C om sidste plads samtidig → én får den, den anden får konfliktbeskeden og ingen delvis tilmelding.
    7. B kan ikke afmelde sig (ingen knap, POST afvises). Admin fjerner B fra én vagt → pladsen er ledig igen live.
    8. Påmindelser med kort interval: 48 t og 24 t modtages én gang, også efter genstart.
    9. Dashboard, deltagerliste og anonymisering af en testbruger.
    10. B svarer på påmindelsesmailen fra sin mailklient → svaret står i **Indbakke** med B's navn. Admin svarer fra systemet, og B modtager svaret i samme tråd.
  - Scenariet køres lokalt med `dotnet run`, da projektet kun er forberedt til Docker.
  - Test på rigtige enheder, kun hvis appen kører med https et sted: Android (Chrome) og iPhone (Safari, føjet til hjemmeskærmen). Installation, push og påmindelser. Ellers står de som ikke testet.
  - Fejl, der findes, rettes i dette step med en regressionstest.
  - `krav.md`: Status for K01–K21 sættes til `Godkendt (step 17)` med henvisning til test/scenarie, eller står som åben med årsag.
  - Manualen gennemlæses som helhed. Afsnittet **Kom i gang** for en ny administrator. Ingen dev-data, test-brugere eller placeholder-links i drift.
- **Ikke med:** Nye funktioner. Udrulning på en rigtig server uden brugerens godkendelse.

## Plan
1. [ ] Automatisk kørsel og rettelse af fejl.
2. [ ] Scenariet "Skolefest" trin for trin, med resultat (OK/fejl) og skærmbilleder pr. trin. Testdata oprettes gennem UI'et, ikke som seeder (`dbcontext.md` regel 14).
3. [ ] Test på rigtige enheder (eller notér hvilke platforme, der ikke er testet, og hvorfor).
4. [ ] Opdatér `krav.md` (status for alle krav og de negative tests).
5. [ ] Opdatér `web/.claude/CLAUDE.md`: Manuel test (scenariet), Kendte afvigelser i koden.
6. [ ] Gennemlæs og færdiggør manualen: alle afsnit, skærmbilleder, **Manglende billeder**, ændringslog og `.manuals/README.md`.
7. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 17 - Samlet test og kravgennemgang" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `.project/krav/krav.md` (status)
- `web/` og `web.Tests/` (kun rettelser og regressionstests)
- `web/.claude/CLAUDE.md`, `.manuals/web/manual.md`, `.manuals/web/images/`

## Beslutninger
- Resultatet af scenariet skrives i **Udført** i denne fil i stedet for i en separat `UAT_REPORT.md` (`planning.md`: "Udført" er historikken) (foreslået af Claude).

## Test
- Som beskrevet under Omfang. Alle punkter skal have resultatet OK, eller være beskrevet som en åben fejl med årsag.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Test på rigtige telefoner kræver, at appen kører med https på en server. Planen forbereder kun Docker (step 16).
