# Manualer (`.manuals/`)

**Gælder for:** alle projekter i løsningen. Hvert projekt har en manual i Markdown med tekst og billeder, som altid beskriver projektets nuværende tilstand.
**Filer:** `.manuals/README.md` (forklaring og oversigt over manualerne), `.manuals/<projekt>/manual.md` (én manual pr. projekt), `.manuals/<projekt>/images/` (manualens billeder).
**Se også:** `planning.md` (manualen er et fast punkt i hver plan), `project.md` (nyt projekt).

## Formål

Manualen er det sted, hvor brugere, administratorer og drift kan læse, hvad projektet gør, og hvordan det bruges, uden at læse koden. Den holdes opdateret i takt med koden, så den aldrig beskriver noget, der ikke findes mere.

`.manuals/` er en punktum-mappe og er derfor gitignored (`.*/`, se `gitignore.md`). Den følger med, når løsningsmappen kopieres, men ikke ved `git clone`.

## Struktur

```text
<løsningens rod>/
└── .manuals/
    ├── README.md                  ← forklaring + oversigt over alle manualer
    └── <projekt>/                 ← præcis samme navn som projektmappen (små bogstaver)
        ├── manual.md              ← manualen
        └── images/                ← alle billeder til manualen, kun her
            └── <afsnit>-<beskrivelse>.png
```

## Regler

### Hvornår
1. **Nyt projekt:** opret `.manuals/<projekt>/manual.md` og `images/` efter skabelonen nedenfor i samme ændring, som projektet oprettes, og tilføj projektet i oversigten i `.manuals/README.md`.
2. **Hver ændring i et projekt opdaterer manualen i samme ændring.** Ret de afsnit, ændringen påvirker (sider, endpoints, login, konfiguration, secret-navne, drift, fejlfinding), og tilføj en linje i **Ændringslog**. En rent intern ændring uden synlig effekt får kun en kort linje i ændringsloggen ("Intern: …").
3. **Planer:** hvert step i `.project/` har punktet "Opdatér manualen" i sin plan og feltet **Manualer** med de berørte manualer (se `planning.md`). Et step er ikke færdigt, før manualerne er opdateret.
4. **Fjernet projekt:** manualen slettes ikke. Øverst skrives "Projektet er fjernet ÅÅÅÅ-MM-DD (step NN)", og det markeres i oversigten i `.manuals/README.md`.
5. Manualen beskriver altid den **nuværende** tilstand. Historikken står kun i **Ændringslog**. Skriv ikke "nu kan man også …" i brødteksten.

### Indhold
6. Skrevet til læseren, ikke til udvikleren: brugsafsnittene til slutbrugere, administrationsafsnittet til administratorer og drift-afsnittet til den, der installerer og driver systemet. Undgå kodeord og klassenavne i brugsafsnittene.
7. Skrevet på projektets UI-sprog (se `<projekt>/.claude/CLAUDE.md`, ellers dansk). Navne på knapper, menuer og felter skrives præcis, som de står i brugerfladen, i **fed**.
8. Følg afsnittene i skabelonen. Afsnit, der ikke passer til projektets type, udelades (fx "Websider" i et API-projekt og "API" i et web-projekt). Et kombineret projekt har begge.
9. **API-delen:** beskriv hvert endpoint-område med formål, adgang (nøgle/master-nøgle), et eksempel-kald med curl og et kort eksempel-svar. Henvis til Swagger UI for den fulde liste. Kopiér ikke hele OpenAPI-dokumentet ind.
10. **Web-delen:** beskriv hver side eller funktion som trin, brugeren følger, med et skærmbillede, hvor det hjælper.
11. **Secrets:** skriv aldrig rigtige nøgler, adgangskoder eller tokens. Skriv konfigurationsnøglens navn (`ApiKeys:MasterKey`) og hvor værdien sættes. Eksempel-nøgler skrives som `<din-api-nøgle>`.
12. Mermaid-diagrammer (```` ```mermaid ````) må bruges til flows og overblik. De er tekst og skal ikke gemmes i `images/`.

### Billeder
13. Alle billeder ligger i `.manuals/<projekt>/images/`, aldrig ved siden af `manual.md` og aldrig i projektets kode.
14. Filnavn: `<afsnit>-<beskrivelse>.png` med små bogstaver og bindestreger, fx `login-side.png`, `brugere-opret.png`, `swagger-authorize.png`. PNG til skærmbilleder.
15. Indsæt med relativ sti, en beskrivende alt-tekst og en billedtekst i kursiv lige under:
    ```markdown
    ![Login-siden med felterne Brugernavn og Adgangskode](images/login-side.png)
    *Login-siden.*
    ```
16. Skærmbilleder tages af den kørende app med testdata, aldrig med rigtige persondata, nøgler eller adgangskoder. Beskær til det relevante, og hold bredden omkring 1280 px.
17. Kan Claude tage skærmbilledet selv (fx med en headless browser mod den lokale app: `msedge --headless --screenshot=<fil> --window-size=1280,800 <url>` for sider uden login), så gør det. Ellers indsættes en pladsholder, og billedet skrives på listen **Manglende billeder**:
    ```markdown
    <!-- BILLEDE MANGLER: images/brugere-opret.png – formularen Opret bruger, udfyldt med testdata -->
    ```
    Billeder opdigtes aldrig, og der laves ikke mock-ups i stedet for rigtige skærmbilleder.
18. Ændres en side, så skal dens skærmbillede tages igen eller sættes på **Manglende billeder**. Billeder, som manualen ikke længere henviser til, slettes fra `images/`.

## Skabelon: `.manuals/<projekt>/manual.md`

````markdown
# <Visningsnavn>: manual

| | |
|---|---|
| Projekt | `<projekt>` |
| Type | API / Web / Kombineret |
| Sidst opdateret | ÅÅÅÅ-MM-DD (step NN) |

## 1. Overblik
Hvad systemet gør, hvem der bruger det, og de vigtigste funktioner i punktform.

## 2. Kom i gang
- Adresser: websider på `<url>/`, Swagger UI på `<url>/swagger` (kombineret: `<url>/api/swagger`).
- Login (web-delen): hvordan man får en bruger, og hvordan man logger ind.
- Adgang til API'et (API-delen): hvordan man får en API-nøgle, og hvordan den sendes (`x-api-key`).

## 3. Websider
### 3.1 <Funktion>
1. Gå til **<Menu>**.
2. Klik på **<Knap>**.

![<alt-tekst>](images/<afsnit>-<beskrivelse>.png)
*<Billedtekst>.*

## 4. API
### 4.1 <Område>
Formål og adgang.

```bash
curl -H "x-api-key: <din-api-nøgle>" <url>/<Navn>/<Action>
```

```json
{ "…": "…" }
```

## 5. Administration
Brugere og roller (web-delen), API-nøgler og rollover (API-delen), log-søgning.

## 6. Konfiguration og drift
| Indstilling | Betydning | Secret |
|---|---|---|
| `<Sektion>:<Nøgle>` | … | Ja/Nej |

- Data: `app_dbs/` og `app_files/` skal tages backup af (i Docker: volumes).
- Opstart: databasen migreres automatisk.
- Docker: port og miljø, hvis projektet kører i Docker.

## 7. Fejlfinding
| Symptom | Årsag | Løsning |
|---|---|---|

## 8. Ændringslog
| Dato | Step | Ændring |
|---|---|---|
| ÅÅÅÅ-MM-DD | NN | … |

## Manglende billeder
- Ingen
````

## Skabelon: oversigten i `.manuals/README.md`

```markdown
| Projekt | Type | Manual | Sidst opdateret |
|---|---|---|---|
| `<projekt>` | API / Web / Kombineret | [manual.md](<projekt>/manual.md) | ÅÅÅÅ-MM-DD |
```

Oversigtens dato opdateres, hver gang en manual opdateres.

## Kendte afvigelser

Ingen.
