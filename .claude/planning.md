# Planlægning i steps (`.project/`)

**Gælder for:** hele løsningen. Alle større ændringer planlægges og dokumenteres som steps i `.project/` i løsningens rod.
**Filer:** `.project/INDEX.md` (oversigten, indlæses automatisk via `CLAUDE.md`), `.project/README.md` (forklaring til mennesker), `.project/steps/step-<NN>-<kort-navn>.md` (ét step pr. fil).

## Formål

- **Gennemsigtighed:** Hver større ændring har en plan, før den laves, og en beskrivelse af, hvad der faktisk blev gjort, når den er færdig.
- **Kontinuitet:** `INDEX.md` fortæller ved hver session, hvad der blev lavet sidst, hvad der er i gang, og hvad der er det næste step. Så kan "næste step" eller "fortsæt" bruges uden yderligere forklaring.
- **Dokumentation:** Afsnittene "Udført" i step-filerne er historikken. Manualerne i `.manuals/` beskriver den nuværende tilstand og opdateres i hvert step (se `manuals.md`).

`.project/` er en punktum-mappe og er derfor gitignored (`.*/`, se `gitignore.md`). Den følger med, når template-mappen kopieres, men ikke ved `git clone`.

## Hvornår skal der laves en plan?

Lav en plan (ét eller flere steps), før arbejdet starter, når ændringen er større:

- Et nyt projekt i løsningen, eller en ny løsning ud fra template'en.
- En ny feature, der rammer mere end ét område (fx tabel + repository + controller + views).
- En ny service/integration til et eksternt system.
- Ændringer i authentication, fejlhåndtering, logging eller pipelinen i `Program.cs`.
- En migration, der ændrer eller sletter eksisterende data.
- Docker-opsætning eller andet deployment.
- Ændringer i de globale instrukser (`.claude/`), der ændrer en regel.
- Alt, hvor brugeren beder om en plan.

Ingen plan til små rettelser i én eller få filer, spørgsmål, fejlfinding uden større ændringer eller rene stavefejl. Er du i tvivl, så spørg brugeren, om der skal laves en plan.

## Arbejdsgang

### Ny plan
1. Find næste ledige nummer i `INDEX.md`. Numre genbruges aldrig, heller ikke for annullerede steps.
2. Del opgaven op i steps, når den er for stor til én omgang, eller når dele kan laves og testes hver for sig. Hvert step skal efterlade løsningen i en tilstand, der bygger og virker.
3. Opret én step-fil pr. step efter skabelonen nedenfor med status `Planlagt`. Steps fra samme opgave får samme navn i feltet **Opgave**, og deres rækkefølge skrives i **Afhænger af**. Hvert step, der ændrer et projekt, har altid punktet "Opdatér manualen" i sin plan og de berørte manualer i feltet **Manualer** (se `manuals.md`).
4. Tilføj steppene i tabellen i `INDEX.md`.
5. Vis brugeren et kort resumé af steppene (nummer, titel, afhængigheder), og **vent på godkendelse, før implementeringen starter**. Har brugeren allerede sagt "kør", "lav det" eller lignende i samme besked, så må du gå i gang med det samme.

### Start et step
6. Tjek, at alle steps i **Afhænger af** har status `Færdig`. Er de ikke det, så sig det og start ikke.
7. Sæt status til `I gang` i step-filen og i `INDEX.md`, og sæt **Aktivt step** øverst i `INDEX.md`.
8. Følg planen og sæt kryds (`[x]`) i step-filen, efterhånden som punkterne bliver færdige.
9. Viser planen sig at være forkert, så ret step-filen (Plan og Beslutninger), før du gør noget andet end det, der står. Er afvigelsen stor, så spørg brugeren først.

### Afslut et step
10. Kør testen fra afsnittet **Test**. Byg mindst løsningen (`dotnet build`), og skriv resultatet. Opdatér manualerne i **Manualer** (tekst, billeder og ændringslog med stepnummeret). Et step er ikke færdigt, før de er opdateret.
11. Udfyld **Udført**: hvad blev lavet, hvad betyder det for brugeren og driften (nye endpoints, sider, konfiguration, secrets), afvigelser fra planen og testresultatet. Skriv det, så det kan bruges direkte i en manual eller README.
12. Sæt status til `Færdig` og datoen i **Færdig** i både step-filen og `INDEX.md`. Opdatér **Sidst færdig** og **Næste step** og ryd **Aktivt step**.
13. Afslut med at fortælle brugeren, hvad der blev lavet, og hvad næste step er.

### "Næste step" og "fortsæt"
14. Beder brugeren om "næste step", "fortsæt" eller lignende, så vælg sådan:
    1. Er der et step med status `I gang`, så fortsæt det.
    2. Ellers: det laveste nummer med status `Planlagt`, hvor alle afhængigheder er `Færdig`.
    3. Findes intet, så sig det, og nævn eventuelle steps med status `Blokeret`.
15. Fortæl kort, hvilket step du tager, og hvad det går ud på, og start så efter reglerne ovenfor. Et step, der allerede er godkendt i en tidligere session, kræver ikke ny godkendelse.

### Øvrige regler
16. **Blokeret:** kan et step ikke gøres færdigt (venter på brugeren, et eksternt system, en beslutning), så sæt status `Blokeret` og skriv årsagen under **Åbne punkter**.
17. **Annulleret:** step-filer slettes aldrig. Et step, der ikke skal laves, får status `Annulleret` og en begrundelse.
18. **Ved sessionens start:** `INDEX.md` er indlæst via `CLAUDE.md`. Er der et aktivt step, og handler brugerens første besked ikke om noget andet, så nævn kort, at steppet er i gang.
19. `INDEX.md` og step-filerne skal altid stemme overens. Ret begge i samme ændring.
20. Datoer skrives som `ÅÅÅÅ-MM-DD`. Skriv aldrig secrets i planerne. Brug navnet på konfigurationsnøglen i stedet.
21. Planerne skrives på dansk.
22. Mangler `.project/`, så opret den med `README.md`, `INDEX.md` og `steps/` efter skabelonerne nedenfor.
23. **Template-mappen selv** (instrukssættet uden `.slnx` og projekter): ændringer i instrukserne planlægges i chatten og ikke som steps i `.project/`. `.project/` og `.manuals/` kopieres med ind i hver ny løsning og skal derfor være tomme i template'en.

## Statusværdier

| Status | Betyder |
|---|---|
| `Planlagt` | Beskrevet, men ikke startet. |
| `I gang` | Arbejdet er startet. Højst ét step ad gangen. |
| `Færdig` | Implementeret, testet og "Udført" er skrevet. |
| `Blokeret` | Kan ikke gøres færdigt lige nu. Årsagen står i step-filen. |
| `Annulleret` | Skal ikke laves. Begrundelsen står i step-filen. |

## Navngivning

- Filnavn: `step-<NN>-<kort-navn>.md` med to cifre og et kort navn med små bogstaver og bindestreger, fx `step-01-init-dotnet-template.md`. Efter step 99 fortsætter nummereringen med tre cifre.
- Titel i filen og i `INDEX.md`: `Step <NN> - <Titel>`, fx `Step 01 - Init. .NET-template til projekt`.
- I en ny løsning er Step 01 normalt oprettelsen af løsningen og det første projekt ud fra template'en.

## Skabelon: `.project/steps/step-<NN>-<kort-navn>.md`

````markdown
# Step <NN> - <Titel>

| | |
|---|---|
| Status | Planlagt |
| Opgave | <navnet på den samlede opgave, hvis den er delt i flere steps, ellers samme som titlen> |
| Projekt | <projekt>, … / løsningen |
| Afhænger af | Ingen / Step <NN>, Step <NN> |
| Manualer | `.manuals/<projekt>/manual.md`, … / Ingen (kun hvis intet projekt ændres) |
| Oprettet | ÅÅÅÅ-MM-DD |
| Færdig | – |

## Formål
Hvorfor steppet laves, og hvad brugeren får ud af det.

## Omfang
- **Med:** …
- **Ikke med:** … (og evt. hvilket senere step det hører til)

## Plan
1. [ ] …
2. [ ] …
3. [ ] Opdatér `<projekt>/.claude/CLAUDE.md` og evt. de globale instrukser.
4. [ ] Opdatér manualen `.manuals/<projekt>/manual.md`: berørte afsnit, skærmbilleder, ændringslog og oversigten i `.manuals/README.md`.

## Berørte filer
- `<projekt>/…` (ny / ændret / slettet)

## Beslutninger
- <beslutning>: <begrundelse> (truffet af brugeren / foreslået af Claude)

## Test
Hvordan resultatet verificeres: build, kørsel, kald, sider, der skal tjekkes.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** nye endpoints/sider, konfiguration, secrets, migrationer …
- **Manualer opdateret:** hvilke afsnit, og evt. billeder på listen Manglende billeder
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Ingen
````

## Skabelon: `.project/INDEX.md`

````markdown
# Projektplan: oversigt

Vedligeholdes af Claude efter `.claude/planning.md`. Ret aldrig her uden også at rette step-filen.

**Aktivt step:** Ingen
**Sidst færdig:** Ingen
**Næste step:** Ingen

| Step | Titel | Opgave | Projekt | Status | Afhænger af | Færdig |
|---|---|---|---|---|---|---|
````

Rækker tilføjes nederst i nummerorden, fx:
`| [01](steps/step-01-init-dotnet-template.md) | Init. .NET-template til projekt | Opstart | løsningen | Færdig | Ingen | 2026-11-08 |`

## Kendte afvigelser

Ingen.
