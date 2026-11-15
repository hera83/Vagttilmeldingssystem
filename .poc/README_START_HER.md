# Vagttilmelding – Claude Code POC → brugbart system

**Formål:** En konkret, trinvis implementeringspakke til Claude Code. Slutresultatet er en **driftsklar, browserbaseret PWA** til forældres tilmelding til vagter og administratorers planlægning – ikke kun skærme eller en klikbar prototype.

## Start her (nem brug)

1. Opret en **tom projektmappe** til den nye applikation, og pak denne ZIP ud i den. Åbn mappen i VS Code/Claude Code.
2. Læs `CLAUDE.md`, `KRAV_OG_ACCEPTKRITERIER.md` og `TEKNISK_DESIGN.md`.
3. Giv Claude Code: **`Læs CLAUDE.md og STEP_00_PROJEKTSTART.md. Udfør kun STEP_00 og afslut med statusrapport. Afvent derefter mit GO.`**
4. For hvert næste trin: **`Læs CLAUDE.md samt STEP_XX_....md. Implementér trinnet, kør tests og rapportér resultatet. Afvent mit GO.`**
5. Fortsæt til og med `STEP_20_END_TO_END_GODKENDELSE.md`. **Markér ikke projektet færdigt, før sluttesten er bestået.**

> Pakken indeholder instruktioner og designreferencer. Den indeholder **ikke** en allerede udviklet POC. Claude Code skal implementere applikationen på baggrund af trinene.

## Implementering (valgt baseline)

- **Backend:** ASP.NET Core **.NET 10**, C#, REST API, SignalR, Identity, EF Core.
- **Frontend:** React + TypeScript + Vite, responsiv enterprise-stil og installerbar PWA.
- **Database:** Microsoft SQL Server, migrations med EF Core.
- **Baggrund:** ASP.NET Core worker/background services, outbox, web push/VAPID.
- **Levering:** Docker Compose lokalt og mulighed for drift på IIS/Windows eller Ubuntu/reverse proxy. Same-origin HTTPS.

Valgene er **arkitekturforslag**, ikke krav fra PDF'en. Hvis der reelt mangler en forudsætning, dokumenterer Claude det i `docs/decisions/`; kernekravene må ikke droppes.

## Trinoversigt

| Fase | Trin | Resultat |
|---|---|---|
| Grundlag | STEP_00–02 | Scope, løsning, startbar platform og database |
| Brugere | STEP_03–04 | Login, roller, profiloplysninger |
| Planlægning | STEP_05–07 | Begivenheder, vagter og skjulte forhåndstildelinger |
| Frigivelse | STEP_08–10 | Offentliggørelse, transaktionssikker booking, multivalg |
| Drift for brugere | STEP_11–13 | Mine vagter, PWA/push, automatiske påmindelser |
| Styring | STEP_14–16 | Dashboard, UI-kvalitet, adgangs- og databeskyttelse |
| Kvalitet | STEP_17–18 | Testpakke og gennemført brugertest |
| Produktion | STEP_19–20 | Deployment, backup, endelig godkendelse |

## Originale kilder og mockups

- `Projektbeskrivelse_Original.pdf` er **kravkilden** (3 sider).
- `KRAV_OG_ACCEPTKRITERIER.md` giver Claude et søgbart, entydigt kravoverblik.
- `mockups/` indeholder **7 faktiske PNG-billeder**, refereret fra `DESIGN_GUIDE.md`.
- **Hvis et billede og PDF'en ikke er enige, har PDF'en/kravlisten forrang.**

## Vigtig arbejdspolitik

- Gennemfør kun ét trin ad gangen. Ingen falske 'færdig'-meldinger eller utestede ændringer.
- Foretag aldrig `git push`, produktion-udrulning eller datamigrering i en rigtig database uden eksplicit GO.
- **Claude-instruktioner holdes uden for Git.** `CLAUDE.md`, `.claude/` og `STEP_*.md` ignoreres i projektets `.gitignore`. De ligger med i ZIP'en til lokal styring af Claude Code.
- Tjek `docs/STEP_STATUS.md` efter hvert trin for faktisk status, tests og restpunkter.
