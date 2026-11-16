# Vagttilmelding

Et webbaseret system til at fordele praktiske vagter blandt forældre i en skoleklasse eller institution, fx til en skolefest eller en arbejdslørdag. Det erstatter tråde på Aula, opslagstavler og regneark.

- **Administratorer** opretter brugere, begivenheder og vagter, kan sætte forældre på vagter i det skjulte og åbner for tilmelding, når planen er klar. Dashboardet viser pladser, aktivitet og påmindelser, og deltagerlisten kan hentes til Excel.
- **Forældre** logger ind, udfylder deres profil, vælger ledige vagter (flere ad gangen) og ser deres egne vagter under **Mine vagter**. Pladserne fordeles efter først-til-mølle, og en vagt kan aldrig blive overbooket.
- **Beskeder:** invitation og glemt adgangskode på e-mail, bekræftelse ved tilmelding, push-besked ved åbning for tilmelding og påmindelser 48 og 24 timer før vagten.
- **Indbakke:** administratorer læser og besvarer forældrenes svar i systemets postkasse direkte i systemet.
- Virker i browseren på computer og mobil, kan installeres som app (PWA) uden App Store og Google Play, og er bygget efter WCAG 2.2 AA.

**Teknik:** ASP.NET Core MVC på .NET 10, SQLite med EF Core, SignalR til live-opdatering, Web Push (VAPID), MailKit til SMTP og IMAP og Serilog. Hele systemet er ét web-projekt (`web/`) med testprojektet `web.Tests/`.

## Indhold

1. [Det skal du bruge](#1-det-skal-du-bruge)
2. [Installation med Docker](#2-installation-med-docker)
3. [Udfyld .env](#3-udfyld-env)
4. [https og reverse proxy](#4-https-og-reverse-proxy)
5. [Første login og røgtest](#5-første-login-og-røgtest)
6. [Drift: backup, opdatering og logs](#6-drift-backup-opdatering-og-logs)
7. [Lokal udvikling](#7-lokal-udvikling)
8. [Fejlfinding](#8-fejlfinding)

## 1. Det skal du bruge

| Hvad | Hvorfor |
|---|---|
| En Linux-server med Docker Engine og Docker Compose v2 (`docker compose`) | Systemet kører i én container. |
| Et domæne, fx `vagter.skolen.dk`, med en DNS-post, der peger på serveren | Login-cookien kræver https, og appen (PWA) og push virker kun over https. |
| Port 80 og 443 åbne ind til serveren | Til reverse proxyen og dens certifikat fra Let's Encrypt. |
| En postkasse til systemet hos en mailudbyder med SMTP og IMAP | Alle mails sendes herfra, og svar fra forældrene lander her og vises i **Indbakke**. |
| Udgående https fra serveren | Push-beskeder sendes via browsernes push-tjenester (Google, Microsoft, Mozilla, Apple). |

## 2. Installation med Docker

> **Bemærk:** Docker-opsætningen er forberedt og appen er testet i Production-tilstand, men containeren er endnu ikke bygget eller kørt på en rigtig server. Følg trinene, og kør røgtesten i afsnit 5 bagefter.

Containeren hedder `web`, kører kun http på port **8080**, bruger dansk tid (`Europe/Copenhagen`) og gemmer alle data i to volumes:

| Volume | Indhold |
|---|---|
| `web_app_dbs` | Databasen `app.db`, logdatabasen `log.db` og mappen `keys/` med nøglerne til login-cookies |
| `web_app_files` | Filer, systemet gemmer |

Docker sætter projektnavnet foran, fx `vagttilmeldingssystem_web_app_dbs`. Se de rigtige navne med `docker volume ls`.

### Trin for trin

1. **Hent koden** på serveren:
   ```bash
   git clone https://github.com/hera83/Vagttilmeldingssystem.git
   cd Vagttilmeldingssystem
   ```

2. **Opret `.env`** ud fra eksemplet, og beskyt filen, så kun du kan læse den. Den kommer til at indeholde adgangskoder og nøgler og må aldrig lægges i git:
   ```bash
   cp .env.example .env
   chmod 600 .env
   ```

3. **Lav nøgler til push-beskeder** (VAPID). Kommandoen bygger image'et første gang, skriver et nøglepar i konsollen og stopper igen:
   ```bash
   docker compose run --rm web --generate-vapid-keys
   ```
   Kopiér de to værdier over i `Push__PublicKey` og `Push__PrivateKey` i `.env`. Lav kun nøglerne én gang: skiftes de senere, skal brugerne slå push til igen.

4. **Udfyld resten af `.env`** (afsnit 3).

5. **Sæt en reverse proxy op** med https foran systemet (afsnit 4).

6. **Byg og start:**
   ```bash
   docker compose up -d --build
   ```
   Databasen oprettes og opdateres automatisk ved opstart, og den første administrator oprettes ud fra `.env`.

7. **Log ind og kør røgtesten** (afsnit 5).

## 3. Udfyld .env

`.env.example` indeholder alle nøgler med en kort forklaring. Nøglerne bruger .NET's notation med dobbelt underscore, så `Mail__Smtp__Host` svarer til indstillingen `Mail:Smtp:Host` i `web/appsettings.json`.

> Indeholder en værdi `$`, `#`, mellemrum eller anførselstegn (typisk en adgangskode), så sæt den i enkelte anførselstegn, fx `Mail__Smtp__Password='<din-adgangskode>'`. Ellers kan Docker Compose læse den forkert.

### Første administrator

| Nøgle | Udfyld med | Påkrævet | Secret |
|---|---|---|---|
| `Users__InitialAdmin__UserName` | Administratorens e-mailadresse. Brugernavne i systemet er altid e-mailadresser. | Ja | Nej |
| `Users__InitialAdmin__Password` | En midlertidig adgangskode på mindst 12 tegn, fx fra `openssl rand -base64 18`. | Ja | **Ja** |

Administratoren oprettes kun ved første opstart, når der endnu ingen brugere er. Skift adgangskoden efter første login, og fjern derefter `Users__InitialAdmin__Password` fra `.env`.

### Domæne

| Nøgle | Udfyld med | Påkrævet | Secret |
|---|---|---|---|
| `App__Domain` | Kun domænet, brugerne åbner systemet på, fx `vagter.skolen.dk`. Uden `https://` og uden `/`. | Ja | Nej |

Systemet sætter selv `https://` foran og bruger adressen til links i mails (invitation, glemt adgangskode, bekræftelser og påmindelser). Uden domænet kan der ikke sendes invitationer eller links til ny adgangskode. Systemet svarer kun på domænet (og på `localhost`, så det kan tjekkes fra selve serveren), og åbnes det på en anden adresse, svarer det **400 Bad Request**.

Den reverse proxy foran (afsnit 4) skal ikke sættes op i `.env`. Systemet stoler automatisk på en proxy på samme server, i Docker eller på jeres lokale netværk.

### Push-beskeder

| Nøgle | Udfyld med | Påkrævet | Secret |
|---|---|---|---|
| `Push__PublicKey` | Den offentlige nøgle fra `--generate-vapid-keys` (trin 3). | For push | Nej |
| `Push__PrivateKey` | Den private nøgle fra samme kommando. | For push | **Ja** |
| `Push__Subject` | En kontaktadresse til push-tjenesterne: `mailto:<e-mail>` eller `https://<adresse>`. | For push | Nej |

Mangler en af de tre, er push slået fra, og resten af systemet virker stadig.

### E-mail og Indbakke

Opret én postkasse til systemet hos jeres mailudbyder, og brug den både til afsendelse (SMTP) og læsning (IMAP). Så lander forældrenes svar i **Indbakke**.

| Nøgle | Udfyld med | Standard | Secret |
|---|---|---|---|
| `Mail__FromAddress` | Systemets postkasse, fx `vagter@skolen.dk`. Alle mails sendes herfra. | – | Nej |
| `Mail__FromName` | Afsendernavnet, modtagerne ser. | `Vagttilmelding` | Nej |
| `Mail__PickupDirectory` | **Lad den være tom i drift.** Kun til udvikling: mails gemmes som filer i stedet for at blive sendt. | tom | Nej |
| `Mail__Smtp__Host` | Udbyderens SMTP-server. | – | Nej |
| `Mail__Smtp__Port` og `Mail__Smtp__Security` | `587` med `StartTls`, `465` med `SslOnConnect` eller `None` (ukrypteret, frarådes). | `587` / `StartTls` | Nej |
| `Mail__Smtp__UserName` | Login til SMTP, typisk postkassens adresse. | – | Nej |
| `Mail__Smtp__Password` | Adgangskoden (eller app-adgangskoden) til SMTP. | – | **Ja** |
| `Mail__Imap__Host` | Udbyderens IMAP-server. | – | Nej |
| `Mail__Imap__Port` og `Mail__Imap__Security` | `993` med `SslOnConnect` eller `143` med `StartTls`. | `993` / `SslOnConnect` | Nej |
| `Mail__Imap__UserName` | Login til IMAP, samme postkasse som `Mail__FromAddress`. | – | Nej |
| `Mail__Imap__Password` | Adgangskoden til IMAP. | – | **Ja** |
| `Mail__Imap__SaveSentCopy` | Gem en kopi af mails skrevet i **Indbakke** i mappen Sendt. Sæt `false`, hvis udbyderen selv gemmer sendte mails (fx Gmail). | `true` | Nej |
| `Mail__Imap__SentFolder` og `Mail__Imap__TrashFolder` | Navnene på Sendt og Papirkurv. Bruges kun, hvis mailserveren ikke selv oplyser dem. | `Sent` / `Trash` | Nej |
| `Mail__Imap__PollIntervalSeconds` | Hvor ofte antal ulæste hentes til dashboardet (sekunder). | `300` | Nej |

**Eksempel med Gmail:** slå 2-trinsbekræftelse til på kontoen, og lav en app-adgangskode, som bruges i begge `Password`-felter. SMTP: `smtp.gmail.com`, `587`, `StartTls`. IMAP: `imap.gmail.com`, `993`, `SslOnConnect`. Sæt `Mail__Imap__SaveSentCopy=false`. Andre udbydere oplyser værdierne i deres vejledning til mailprogrammer.

### Påmindelser, begrænsninger og oprydning

Standardværdierne passer til de fleste og kan blive stående.

| Nøgle | Betydning | Standard |
|---|---|---|
| `Reminders__Enabled` | Påmindelser 48 og 24 timer før vagtstart på push og e-mail. | `true` |
| `Reminders__WorkerIntervalSeconds` | Hvor ofte der tjekkes for påmindelser. En påmindelse kan komme op til ét interval senere. | `3600` (hver time) |
| `RateLimiting__Enabled` | Begrænsning af forsøg. Slå det kun fra til test. | `true` |
| `RateLimiting__LoginPermitLimit` / `LoginWindowSeconds` | Login-forsøg pr. IP-adresse. | 10 pr. 300 sek. |
| `RateLimiting__PasswordPermitLimit` / `PasswordWindowSeconds` | **Glemt adgangskode** og links til ny adgangskode pr. IP-adresse. | 5 pr. 900 sek. |
| `RateLimiting__EnrollPermitLimit` / `EnrollWindowSeconds` | Tilmeldinger pr. bruger. | 20 pr. 60 sek. |
| `DataRetention__NotificationDays` | Antal dage, sendte og opgivne beskeder gemmes. | `90` |

### Eksempel på en udfyldt .env

Alle værdier er eksempler. Erstat dem med jeres egne.

```dotenv
Users__InitialAdmin__UserName=admin@skolen.dk
Users__InitialAdmin__Password='<midlertidig-adgangskode-på-mindst-12-tegn>'

Push__PublicKey=<offentlig-nøgle-fra-generate-vapid-keys>
Push__PrivateKey=<privat-nøgle-fra-generate-vapid-keys>
Push__Subject=mailto:admin@skolen.dk

App__Domain=vagter.skolen.dk

Mail__FromAddress=vagter@skolen.dk
Mail__FromName=Vagttilmelding
Mail__PickupDirectory=

Mail__Smtp__Host=smtp.<udbyder>.dk
Mail__Smtp__Port=587
Mail__Smtp__Security=StartTls
Mail__Smtp__UserName=vagter@skolen.dk
Mail__Smtp__Password='<adgangskode-til-postkassen>'

Mail__Imap__Host=imap.<udbyder>.dk
Mail__Imap__Port=993
Mail__Imap__Security=SslOnConnect
Mail__Imap__UserName=vagter@skolen.dk
Mail__Imap__Password='<adgangskode-til-postkassen>'
Mail__Imap__SaveSentCopy=true
Mail__Imap__SentFolder=Sent
Mail__Imap__TrashFolder=Trash
Mail__Imap__PollIntervalSeconds=300

Reminders__Enabled=true
Reminders__WorkerIntervalSeconds=3600

RateLimiting__Enabled=true
RateLimiting__LoginPermitLimit=10
RateLimiting__LoginWindowSeconds=300
RateLimiting__PasswordPermitLimit=5
RateLimiting__PasswordWindowSeconds=900
RateLimiting__EnrollPermitLimit=20
RateLimiting__EnrollWindowSeconds=60

DataRetention__NotificationDays=90
```

Efter en ændring i `.env`: `docker compose up -d` (genskaber containeren med de nye værdier).

## 4. https og reverse proxy

Systemet kører kun http i containeren. En reverse proxy foran skal:

- stå for https-certifikatet,
- sende WebSocket-forbindelser videre (live-opdatering på `/hubs/shifts`),
- sende headerne `X-Forwarded-For` og `X-Forwarded-Proto`.

Systemet stoler på de headere, når de kommer fra samme server, fra Docker eller fra et privat netværk (10.x, 172.16–31.x, 192.168.x). Der skal ikke sættes noget i `.env`.

Port `8080` er i `docker-compose.yml` kun åben fra selve serveren (`127.0.0.1:8080`), så systemet ikke kan nås uden om proxyen. Det passer til begge eksempler nedenfor. Kører proxyen på en anden maskine (fx en NAS eller router), så skift linjen til `"8080:8080"`, og sørg med firewallen for, at kun proxyen kan nå porten.

### Caddy i samme compose-fil (anbefalet)

Caddy henter selv et certifikat fra Let's Encrypt og sender WebSockets og `X-Forwarded-*` videre af sig selv. Tilføj under `services:` og `volumes:` i `docker-compose.yml`:

```yaml
  caddy:
    image: caddy:2
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - caddy_data:/data
    environment:
      - TZ=Europe/Copenhagen
    restart: unless-stopped

volumes:
  caddy_data:
```

og lav filen `Caddyfile` ved siden af `docker-compose.yml`:

```text
vagter.skolen.dk {
    reverse_proxy web:8080
}
```

### nginx på serveren

```nginx
map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
}

server {
    listen 80;
    server_name vagter.skolen.dk;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name vagter.skolen.dk;
    ssl_certificate     /etc/letsencrypt/live/vagter.skolen.dk/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/vagter.skolen.dk/privkey.pem;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $connection_upgrade;
        proxy_read_timeout 120s;
    }
}
```

**En proxy med en offentlig IP-adresse** (fx hos en hostingudbyder) er ikke betroet af sig selv. Sæt så dens adresse i `ForwardedHeaders__KnownProxies` (flere adskilles med komma) eller et netværk i `ForwardedHeaders__KnownNetworks`, fx `203.0.113.0/24`.

## 5. Første login og røgtest

**Første login**
1. Åbn `https://<jeres-domæne>/`, og log ind med `Users__InitialAdmin__UserName` og `Users__InitialAdmin__Password`.
2. Skift adgangskoden under **Skift adgangskode** (mindst 12 tegn).
3. Fjern `Users__InitialAdmin__Password` fra `.env`, og kør `docker compose up -d`.
4. Opret forældrene under **Brugere** → **Opret bruger** med **Send invitation på e-mail**. De vælger selv adgangskode via linket i mailen.

**Røgtest**
1. `docker compose ps`: `web` kører. `docker compose logs web`: ingen fejl. Ved første start er disse advarsler forventede: "No XML encryptor configured" (nøglerne ligger ukrypteret i volumen), "Failed to determine the https port for redirect" (https håndteres af proxyen) og advarsler om push, mail eller `App:Domain`, hvis de ikke er sat.
2. `docker compose exec web date`: klokken er dansk tid (CET/CEST).
3. Login-siden vises med hængelås i browseren.
4. **Dashboard** → **Beskeder og påmindelser** viser, at push og e-mail er sat op.
5. Opret en testbruger med invitation: mailen kommer frem, og linket starter med `https://` og jeres domæne.
6. **Min profil** → **Slå push til** → **Send testbesked**: beskeden kommer frem.
7. Åbn **Ledige vagter** i to browsere, og åbn for en testvagt som administrator: vagten dukker op uden genindlæsning (WebSocket gennem proxyen).
8. `docker compose restart web`: du er stadig logget ind bagefter.
9. Slet testbrugeren og testbegivenheden igen.

## 6. Drift: backup, opdatering og logs

### Nyttige kommandoer

| Kommando | Gør |
|---|---|
| `docker compose ps` | Viser status. |
| `docker compose logs -f web` | Følger loggen. |
| `docker compose restart web` | Genstarter systemet. |
| `docker compose down` | Stopper systemet. Volumes og data bevares. |

Loggen skrives også til `log.db` i volumen `web_app_dbs` og holdes automatisk under 1 GB.

### Backup

Tag backup af de to volumes og af `.env`. Mails ligger hos mailudbyderen og tages backup af dér. Stop systemet under backuppen, så databasen og dens `-wal`- og `-shm`-filer passer sammen. Erstat `<projekt>` med præfikset fra `docker volume ls` (typisk `vagttilmeldingssystem`).

```bash
docker compose stop web
docker run --rm -v <projekt>_web_app_dbs:/data -v "$PWD":/backup alpine tar czf /backup/app_dbs-$(date +%F).tgz -C /data .
docker run --rm -v <projekt>_web_app_files:/data -v "$PWD":/backup alpine tar czf /backup/app_files-$(date +%F).tgz -C /data .
docker compose start web
```

Gem `.tgz`-filerne og en kopi af `.env` et sikkert sted uden for serveren. De indeholder persondata og nøgler. Uden mappen `keys/` i backuppen bliver alle brugere logget ud efter en gendannelse.

### Gendannelse

Containeren kører som brugeren med id `1654`, så filerne skal ejes af den:

```bash
docker compose stop web
docker run --rm -v <projekt>_web_app_dbs:/data -v "$PWD":/backup alpine sh -c "rm -rf /data/* && tar xzf /backup/app_dbs-<dato>.tgz -C /data && chown -R 1654:1654 /data"
docker run --rm -v <projekt>_web_app_files:/data -v "$PWD":/backup alpine sh -c "rm -rf /data/* && tar xzf /backup/app_files-<dato>.tgz -C /data && chown -R 1654:1654 /data"
docker compose start web
```

### Opdatering

1. Tag en backup.
2. Hent den nye version, og byg igen:
   ```bash
   git pull
   docker compose up -d --build
   ```
   Databasen opdateres automatisk ved opstart, og brugerne forbliver logget ind.
3. Kør røgtesten (afsnit 5).

**Tilbagerulning:** en ny version kan have ændret databasen, så den gamle version ikke kan læse den. Gå tilbage til den forrige version (`git checkout <forrige version>`), gendan backuppen fra før opdateringen, og kør `docker compose up -d --build`.

## 7. Lokal udvikling

**Krav:** .NET 10 SDK. Til database-migrationer også `dotnet tool install --global dotnet-ef`.

1. Opret `web/appsettings.Development.json`. Filen er gitignored, fordi den indeholder secrets, og kommer aldrig med i en publish eller et Docker-image. Et minimum:
   ```json
   {
     "Users": {
       "InitialAdmin": {
         "UserName": "admin@vagttilmelding.local",
         "Password": "<lokal-adgangskode-på-mindst-12-tegn>"
       }
     },
     "Mail": {
       "FromAddress": "vagter@vagttilmelding.local",
       "PickupDirectory": "app_files/MailPickup"
     }
   }
   ```
   Links i mails bruger af sig selv appens egen http-adresse fra launch-profilen (`http://localhost:5180`), så `App:Domain` skal ikke sættes lokalt. Med `Mail:PickupDirectory` gemmes mails som `.eml`-filer i `web/app_files/MailPickup/` i stedet for at blive sendt. Push kræver et lokalt nøglepar: `dotnet run --project web -- --generate-vapid-keys`, og sæt `Push:PublicKey`, `Push:PrivateKey` og `Push:Subject`. **Indbakke** kræver en rigtig postkasse i `Mail:Imap:*`.
2. Start systemet:
   ```bash
   dotnet run --project web
   ```
   Åbn `http://localhost:5180` (eller `https://localhost:7180` med `--launch-profile https`), og log ind med `Users:InitialAdmin`. Databasen oprettes i `web/app_dbs/` ved første start.
3. Kør testene:
   ```bash
   dotnet test
   ```
   Stop appen først, ellers fejler buildet, fordi `web.dll` er låst.

| Mappe | Indhold |
|---|---|
| `web/` | Web-projektet: controllere, views, managers, repositories, services, baggrundsjob og databasen (`data/`) |
| `web.Tests/` | Integrationstests (xUnit) |
| `docker-compose.yml`, `web/Dockerfile`, `.dockerignore` | Docker-opsætningen |
| `.env.example` | Alle indstillinger til drift med tomme værdier |

## 8. Fejlfinding

| Symptom | Årsag | Løsning |
|---|---|---|
| Ingen kan logge ind efter første opstart | `Users__InitialAdmin__*` var ikke sat, da databasen blev oprettet | Sæt begge værdier. Er der endnu ingen data, så stop systemet, slet volumen `<projekt>_web_app_dbs`, og start igen. |
| **400 Bad Request** | Systemet er åbnet på en anden adresse end `App__Domain`, fx serverens IP-adresse | Brug domænet. Tjek, at `App__Domain` er stavet rigtigt, og at proxyen sender den oprindelige `Host` videre. |
| "Der opstod en fejl", når login-siden åbnes | Siden er åbnet direkte over http (fx `http://<server>:8080`) uden om proxyen, eller proxyen sender ikke `X-Forwarded-Proto` | Åbn systemet via `https://` og domænet. Tjek proxyens headers. |
| Login virker ikke eller **For mange forsøg** for mange brugere på én gang bag en proxy på en anden maskine | Proxyen har en offentlig IP-adresse og er derfor ikke betroet | Sæt `ForwardedHeaders__KnownProxies` til proxyens adresse (afsnit 4), og kør `docker compose up -d`. |
| Systemet starter ikke: "App:Domain indeholder …" | Der står mere end domænet, fx en sti | Skriv kun domænet, fx `vagter.skolen.dk`. |
| Systemet starter ikke: "ForwardedHeaders:KnownNetworks indeholder …" | Ugyldig værdi | Brug IP-adresser i `KnownProxies` og netværk med `/` i `KnownNetworks`, fx `203.0.113.0/24`. |
| "Der kan ikke sendes e-mails" ved **Opret bruger** | `Mail__FromAddress`, `Mail__Smtp__Host` eller `App__Domain` mangler | Udfyld dem, og kør `docker compose up -d`. Indtil da kan brugere oprettes med en adgangskode. |
| Loggen siger "SMTP-serveren … afviste login" | Forkert `Mail__Smtp__UserName` eller `Password` | Ret værdierne. Hos Gmail og lignende skal der bruges en app-adgangskode. |
| Loggen siger "Kunne ikke forbinde til SMTP-serveren" | Forkert host, port eller sikkerhed, eller en firewall blokerer | Brug `587` med `StartTls` eller `465` med `SslOnConnect`. |
| Mails kommer ikke frem, men der er ingen fejl | `Mail__PickupDirectory` er sat | Tøm værdien i drift. |
| **Indbakke** viser "Webmailen er ikke sat op" eller "Postkassen kunne ikke nås" | `Mail__Imap__*` mangler eller er forkert | Fejlteksten nævner de indstillinger, der skal tjekkes. |
| **Min profil** viser **Ikke sat op** ved push | `Push__PublicKey`, `Push__PrivateKey` eller `Push__Subject` mangler | Lav nøgler med `--generate-vapid-keys` (trin 3), sæt de tre værdier, og kør `docker compose up -d`. |
| "Forbindelsen er afbrudt – prøver igen …" bliver stående | Proxyen sender ikke WebSockets videre | Tillad WebSockets (headerne `Upgrade` og `Connection`) i proxyen. Siderne virker stadig ved genindlæsning. |
| Alle brugere er logget ud efter en opdatering | Nøglerne i `app_dbs/keys` er væk, fordi volumen mangler eller er ny | Tjek volumes med `docker compose config`, og gendan `app_dbs` fra backup. |
| "Der opstod en fejl" med et fejl-id | Uventet fejl på serveren | Find fejl-id'et i `docker compose logs web`. |
