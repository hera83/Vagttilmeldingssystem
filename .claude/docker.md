# Docker

**Gælder for:** Docker-opsætning af løsningen. Laves **kun**, når brugeren beder om en docker-opsætning.
**Se også:** `configuration.md` (hvilke værdier er secrets), `databases.md` og `files.md` (data der skal overleve), `gitignore.md` (.env).

## Filer

En docker-opsætning består altid af:

```text
<løsningens rod>/
├── docker-compose.yml        ← alle projekter som services
├── .dockerignore             ← holder secrets, build-output og data ude af images
├── .env.example              ← committes: alle nøgler med tomme/eksempel-værdier
├── .env                      ← committes ALDRIG (gitignored): de rigtige værdier
└── <projekt>/Dockerfile      ← ét pr. projekt, fx api/Dockerfile
```

## Regler

### Porte
1. Hvert projekt bruger **samme port inde i containeren og på værten**.
2. API-projekter: 5000, 5001, 5002 … Web- og kombinerede projekter: 8080, 8081, 8082 … Nummereringen følger projekternes rækkefølge i `.slnx` inden for hver serie.
3. Porten sættes i Dockerfile med `ENV ASPNETCORE_HTTP_PORTS=<port>` og `EXPOSE <port>` og mappes i compose som `"<port>:<port>"`.
4. Containeren kører kun HTTP. TLS håndteres af en reverse proxy foran. Advarslen fra `UseHttpsRedirection` om manglende https-port er forventet. Web-delen kræver https foran: login-cookien er `Secure` i Production, så browseren sender den kun over https.

### Data
5. `app_dbs/` og `app_files/` mountes **altid** som named volumes (`<service>_app_dbs` og `<service>_app_files`). Ellers forsvinder database og filer, når containeren genskabes.
6. Dockerfile opretter `/app/app_dbs` og `/app/app_files` og giver dem til `$APP_UID`, før der skiftes til non-root-brugeren. Så arver nye volumes de rigtige rettigheder.

### Secrets og konfiguration
7. Opret altid `.env.example`, når projektet har secrets eller miljøspecifikke værdier (API-nøgler, passwords, base-URL'er …). Den indeholder alle nøgler med tomme værdier og en kommentar om at kopiere filen til `.env`.
8. Nøglerne i `.env` skrives i .NET's notation med dobbelt underscore, så de rammer konfigurationen direkte: `ApiKeys__MasterKey=…` svarer til `ApiKeys:MasterKey`.
9. Compose indlæser værdierne med `env_file: .env` og sætter `ASPNETCORE_ENVIRONMENT=Production`.
10. `.env`, `appsettings.Development.json`, `bin/`, `obj/`, `app_dbs/`, `app_files/`, punktum-mapper og `CLAUDE.md` står i `.dockerignore` og må aldrig komme med i et image.

### Tidszone
11. Alle containere kører **altid** i dansk tid. Sæt `ENV TZ=Europe/Copenhagen` i den endelige stage i alle Dockerfiles. Services i compose, der bruger et færdigt image uden egen Dockerfile, får `TZ=Europe/Copenhagen` under `environment`.
12. `TZ` virker kun, når image'et har tidszonedata (`tzdata`). Microsofts standard-images til .NET 10 (`aspnet:10.0`, Ubuntu 24.04) har det med. Chiseled-images har det kun i `-extra`-varianten, og andre images skal tjekkes. Uden `tzdata` falder .NET stille tilbage til UTC.
13. Tidszonen styrer `DateTime.Now`, `TimeZoneInfo.Local`, konsollens log-tidsstempler og job, der skal køre på et bestemt klokkeslæt. Den ændrer **ikke** reglen om, at tidspunkter gemmes i UTC med `DateTime.UtcNow` (se `databases.md`).

## Skabeloner (API-projekt nr. 1, port 5000)

```dockerfile
# <projekt>/Dockerfile — bygges med løsningens rod som context
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY <projekt>/<projekt>.csproj <projekt>/
RUN dotnet restore <projekt>/<projekt>.csproj
COPY . .
RUN dotnet publish <projekt>/<projekt>.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/app_dbs /app/app_files && chown -R $APP_UID /app/app_dbs /app/app_files
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=5000
ENV TZ=Europe/Copenhagen
EXPOSE 5000
ENTRYPOINT ["dotnet", "<projekt>.dll"]
```

```yaml
# docker-compose.yml
services:
  <projekt>:
    build:
      context: .
      dockerfile: <projekt>/Dockerfile
    ports:
      - "5000:5000"
    env_file: .env
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - <projekt>_app_dbs:/app/app_dbs
      - <projekt>_app_files:/app/app_files
    restart: unless-stopped

volumes:
  <projekt>_app_dbs:
  <projekt>_app_files:
```

```gitignore
# .dockerignore
**/bin
**/obj
**/.vs
**/.vscode
**/.idea
.git
.claude
.project
.manuals
CLAUDE.md
**/appsettings.Development.json
**/app_dbs
**/app_files
**/*.user
.env
```

## Tjekliste

1. Én Dockerfile pr. projekt med den rigtige port (regel 1-3).
2. Volumes til `app_dbs` og `app_files` (regel 5-6).
3. `.env.example` med **alle** secrets fra `appsettings.Development.json` i `__`-notation (regel 7-8).
4. `.dockerignore` (regel 10).
5. Tjek, at `.env` er gitignored (se `gitignore.md`).
6. `TZ=Europe/Copenhagen` i alle containere (regel 11-12). Tjek med `docker compose exec <service> date`, at klokken er dansk (CET/CEST).

## Kendte afvigelser i koden

Ingen. Om løsningen har en docker-opsætning, og hvilket portnummer hvert projekt har, står i `<projekt>/.claude/CLAUDE.md` (Nøgletal → Docker).
