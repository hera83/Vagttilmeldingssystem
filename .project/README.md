# `.project/`: planer og historik

Her ligger planerne for alle større ændringer i løsningen. Hver ændring er delt op i **steps**, og hvert step har sin egen fil. Mappen er en punktum-mappe, så den er gitignored og kun findes lokalt (den følger med, når template-mappen kopieres).

## Indhold

| Fil | Indhold |
|---|---|
| `INDEX.md` | Oversigten: alle steps med status og afhængigheder, det aktive step, det sidst færdige og det næste. Claude læser den automatisk ved hver session. |
| `steps/step-<NN>-<kort-navn>.md` | Ét step: formål, omfang, plan med afkrydsning, beslutninger, test og "Udført". |
| `krav/` | Kravgrundlaget for opgaven Vagttilmeldingssystem: `krav.md` (krav K01–K21 med status, forretningsregler, beslutninger), den originale projektbeskrivelse (PDF) og `mockups/`. Kopieret fra `.poc/` 2026-11-15. |
| `README.md` | Denne fil. |

## Sådan bruges det

| Du siger | Claude gør |
|---|---|
| Beder om en større ændring | Laver en plan i ét eller flere steps, viser et resumé og venter på din godkendelse. |
| "kør", "godkendt", "lav det" | Starter det første step i planen. |
| "næste step" eller "fortsæt" | Fortsætter det step, der er i gang, eller tager det laveste planlagte step, hvor alle afhængigheder er færdige. |
| "lav en plan for …" | Laver kun planen, uden at implementere. |
| "annullér step NN" | Sætter steppet til Annulleret. Filen slettes ikke. |

Et step har en af disse status: **Planlagt**, **I gang**, **Færdig**, **Blokeret** eller **Annulleret**.

## Til manual og README

Når et step er færdigt, udfylder Claude afsnittet **Udført** med det, der faktisk blev lavet: nye endpoints og sider, konfiguration, secrets, migrationer, afvigelser fra planen og testresultatet. Læs steppene i nummerorden for at få hele historikken. Hvert step opdaterer også manualerne for de projekter, det ændrer (se `.manuals/`), så de altid beskriver den nuværende tilstand.

Reglerne står i `.claude/planning.md`.
