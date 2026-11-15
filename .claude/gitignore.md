# .gitignore

**Gælder for:** `.gitignore` i roden af løsningen. Én fil dækker alle projekter i løsningen.
**Se også:** `databases.md` (app_dbs), `files.md` (app_files), `docker.md` (.env).

## Formål

Kun kildekode og nødvendig konfiguration må komme i git. Alt lokalt, kompileret, hemmeligt eller genereret af appen under kørsel skal ignoreres.

## Regler

1. Hold `.gitignore` opdateret **i samme ændring** som det, der kræver det. Tilføjer du et nyt projekt, en ny værktøjsmappe, en ny slags genereret fil eller en ny datamappe, så tjek og opdatér `.gitignore` med det samme.
2. Skriv mønstre uden foranstillet `/` eller `*/`, så de gælder i alle projekter og i alle dybder (`[Bb]in/`, ikke `*/bin/`).
3. Disse ting skal **altid** være ignoreret:

| Hvad | Mønster | Hvorfor |
|---|---|---|
| Alle mapper, der starter med `.` | `.*/` | IDE-, værktøjs- og AI-mapper (`.vs`, `.vscode`, `.idea`, `.claude`, `.github`, `.config` …). **Ingen undtagelser.** |
| AI-instrukser | `CLAUDE.md` (alle dybder) + `.claude/` i roden og i hvert projekt (dækket af `.*/`) | Bevidst lokale. De følger med, når template-mappen kopieres, ikke ved `git clone`. |
| Planer | `.project/` i roden (dækket af `.*/`) | Bevidst lokale, ligesom instrukserne (se `planning.md`). |
| Manualer | `.manuals/` i roden (dækket af `.*/`) | Bevidst lokale, ligesom instrukserne (se `manuals.md`). |
| Kompileret output | `[Bb]in/`, `[Oo]bj/`, `[Oo]ut/`, `*.dll`, `*.pdb` | Build-artefakter. |
| Secrets | `appsettings.Development.json`, `.env`, `.env.*` | Rigtige nøgler og passwords må aldrig i git. |
| Databaser | `**/app_dbs/**` + `*.db`, `*.db-shm`, `*.db-wal` | Se regel 4. |
| Appens filer | `**/app_files/**` | Se regel 5. |
| Lokal støj | `*.user`, `*.suo`, `*.log`, `.DS_Store`, `Thumbs.db`, `*.nupkg` … | IDE, OS, logs og NuGet. |

4. **`app_dbs/`**: alle filer ignoreres. Kun `.gitkeep` må committes, så mappen findes i template'en.
5. **`app_files/`**: alle *filer* ignoreres i alle dybder, men *mappestrukturen* bevares. Opretter du en ny undermappe i `app_files/`, så læg en tom `.gitkeep` i den. Git kan ikke gemme tomme mapper.
6. *Filer*, der starter med punktum, er ikke omfattet af punktum-mappe-reglen og skal committes: `.gitignore`, `.gitkeep`, `.env.example`, `.dockerignore`.
7. Fjern eller svæk aldrig en eksisterende regel for at få en fil med i git. Spørg brugeren først.

## De præcise mønstre for app_dbs og app_files

Rækkefølgen er vigtig: negationerne (`!`) skal stå **efter** det mønster, de undtager fra.

```gitignore
**/app_dbs/**
!**/app_dbs/.gitkeep

**/app_files/**
!**/app_files/**/
!**/app_files/**/.gitkeep

.env
.env.*
!.env.example
```

## Tjek dit arbejde

- `git check-ignore -v <sti>` viser, om en fil er ignoreret, og hvilken linje der rammer den.
- `git status --short` efter en ændring må ikke vise filer fra `bin/`, `obj/`, `app_dbs/`, `app_files/` (bortset fra `.gitkeep`) eller secrets.
