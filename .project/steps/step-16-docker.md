# Step 16 - Forberedelse til Docker

| | |
|---|---|
| Status | Planlagt |
| Opgave | Vagttilmeldingssystem |
| Projekt | løsningen, web |
| Afhænger af | Step 15 |
| Manualer | `.manuals/web/manual.md` |
| Oprettet | 2026-11-15 |
| Færdig | – |

## Formål
Projektet forberedes til at køre i Docker: Dockerfile, compose, `.dockerignore` og en komplet `.env.example`, så det kan sættes op på en server uden ændringer i koden. Containeren bygges og køres **ikke** som en del af planen (truffet af brugeren). Krav: K01 (drift).

## Omfang
- **Med:**
  - Filerne fra `docker.md`: `web/Dockerfile` (port **8080**, første web-projekt), `docker-compose.yml` og `.dockerignore`.
  - Volumes `web_app_dbs` og `web_app_files`. `TZ=Europe/Copenhagen`. `ASPNETCORE_ENVIRONMENT=Production`. `env_file: .env`.
  - **`.env.example`:** gennemgå, at alle secrets og miljøspecifikke nøgler fra `appsettings.json` står der i `__`-notation med tomme værdier. Filen er oprettet i step 01 og opdateret løbende. Forventet indhold: `Users__InitialAdmin__*`, `Push__*`, `Mail__Smtp__*`, `Mail__Imap__*`, `Mail__FromAddress`, `Mail__FromName`, `App__BaseUrl`, `Reminders__*` og evt. `ForwardedHeaders__*`.
  - **Bag en reverse proxy:** `ForwardedHeaders` (`X-Forwarded-For`/`-Proto`) i `Program.cs`, så https-redirect, `Secure`-cookie og links i mails virker, når appen kører bag en proxy med https. Kendte proxies/netværk kan konfigureres.
  - **Manualen** beskriver, hvordan appen sættes op med Docker: `.env` ud fra `.env.example`, `docker compose up -d --build`, reverse proxy med https og websockets (eksempler med Caddy og nginx, da SignalR kræver `Upgrade`-headers), backup og gendannelse af volumes (`app_dbs/` inkl. `-wal`/`-shm`, `app_files/`, `.env`), opdatering og tilbagerulning og en røgtest efter opsætning. Det markeres tydeligt som **ikke afprøvet**.
- **Ikke med:** At bygge eller køre containeren, udrulning på en server, opsætning af domæne og certifikat og afprøvning af backup/gendannelse.

## Plan
1. [ ] `web/Dockerfile`, `docker-compose.yml` og `.dockerignore` efter skabelonerne og tjeklisten i `docker.md`.
2. [ ] Gennemgå `.env.example` mod `appsettings.json` og ret mangler.
3. [ ] `ForwardedHeaders` i `Program.cs` (først i pipelinen) med konfiguration i `appsettings.json` og `.env.example`. Test, at `Request.Scheme` bliver `https`, når `X-Forwarded-Proto: https` kommer fra en kendt proxy, og ikke ellers.
4. [ ] Tjek, at `.env` er gitignored (`git check-ignore -v .env`), og at `.dockerignore` udelukker secrets, `bin/`, `obj/`, `app_dbs/`, `app_files/`, punktum-mapper og `CLAUDE.md`.
5. [ ] Kør `dotnet publish web -c Release` (samme kommando som i Dockerfile) til en midlertidig mappe for at sikre, at image-buildet har noget at kopiere.
6. [ ] Opdatér `web/.claude/CLAUDE.md`: Nøgletal → Docker (forberedt, port 8080, ikke kørt), Konfiguration, "Bevidste afvigelser" (`ForwardedHeaders` i `Program.cs`).
7. [ ] Opdatér manualen: **Installation med Docker**, **https og reverse proxy**, **Backup og gendannelse**, **Opdatering og tilbagerulning**, **Røgtest** (markeret ikke afprøvet), ændringslog og `.manuals/README.md`.
8. [ ] Commit og push: tjek med `git status --short`, at der ikke kommer secrets, `bin/`, `obj/` eller filer fra `app_dbs/`/`app_files/` (ud over `.gitkeep`) med. Commit med beskeden "Step 16 - Forberedelse til Docker" og push. Fejler push, så skriv det under Åbne punkter og sig det til brugeren.

## Berørte filer
- `web/Dockerfile`, `docker-compose.yml`, `.dockerignore` (nye)
- `.env.example`, `web/Program.cs`, `web/appsettings.json`, `.gitignore` (ændret)

## Beslutninger
- Projektet forberedes kun til Docker. Containeren bygges og køres ikke i planen (truffet af brugeren).
- Containeren kører kun http på 8080, og https håndteres af en reverse proxy foran (`docker.md` regel 4).

## Test
- `dotnet build`, `dotnet test` og `dotnet publish web -c Release`.
- Gennemgang af filerne mod tjeklisten i `docker.md` og af `.env.example` mod `appsettings.json`.

## Udført
_Udfyldes, når steppet er færdigt._
- **Lavet:** …
- **Betydning for brugeren og driften:** …
- **Manualer opdateret:** …
- **Afvigelser fra planen:** …
- **Testresultat:** …

## Åbne punkter
- Første rigtige kørsel i Docker (og test af backup/gendannelse) sker ved en senere udrulning, når brugeren har valgt server, domæne og reverse proxy.
