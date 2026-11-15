# Projektplan: oversigt

Vedligeholdes af Claude efter `.claude/planning.md`. Ret aldrig her uden også at rette step-filen.

**Aktivt step:** Ingen
**Sidst færdig:** Ingen
**Næste step:** Step 01 - Init. løsning og web-projekt fra template'en
**Krav:** Opgaven Vagttilmeldingssystem bygger på [krav/krav.md](krav/krav.md) (K01–K21, forretningsregler, brugerens beslutninger). Læs den, før et step i opgaven startes.
**Git:** Når et step er færdigt, committes og pushes ændringerne **automatisk** uden at spørge (beskeden "Step NN - Titel"), som sidste punkt i steppets plan. `.project/`, `.manuals/` og `.claude/` er gitignored og kommer ikke med.

| Step | Titel | Opgave | Projekt | Status | Afhænger af | Færdig |
|---|---|---|---|---|---|---|
| [01](steps/step-01-init-loesning-og-web.md) | Init. løsning og web-projekt fra template'en | Vagttilmeldingssystem | løsningen, web | Planlagt | Ingen | – |
| [02](steps/step-02-profiler.md) | Profiler og stamdata | Vagttilmeldingssystem | web | Planlagt | Step 01 | – |
| [03](steps/step-03-begivenheder.md) | Begivenheder (admin) | Vagttilmeldingssystem | web | Planlagt | Step 01 | – |
| [04](steps/step-04-vagter-og-kladder.md) | Vagter og kladder (admin) | Vagttilmeldingssystem | web | Planlagt | Step 03 | – |
| [05](steps/step-05-tildelinger-og-mine-vagter.md) | Tildelinger: forhåndstildeling, fjernelse og Mine vagter | Vagttilmeldingssystem | web | Planlagt | Step 02, Step 04 | – |
| [06](steps/step-06-frigivelse.md) | Frigivelse: Åbn for tilmelding | Vagttilmeldingssystem | web | Planlagt | Step 05 | – |
| [07](steps/step-07-ledige-vagter-og-tilmelding.md) | Ledige vagter og samlet tilmelding | Vagttilmeldingssystem | web | Planlagt | Step 02, Step 06 | – |
| [08](steps/step-08-realtid-signalr.md) | Realtidsopdatering med SignalR | Vagttilmeldingssystem | web | Planlagt | Step 07 | – |
| [09](steps/step-09-pwa.md) | PWA: installérbar app og offline-side | Vagttilmeldingssystem | web | Planlagt | Step 01 | – |
| [10](steps/step-10-web-push.md) | Web push ved frigivelse | Vagttilmeldingssystem | web | Planlagt | Step 08, Step 09 | – |
| [11](steps/step-11-e-mail.md) | E-mail: invitation, glemt adgangskode og bekræftelser | Vagttilmeldingssystem | web | Planlagt | Step 07, Step 10 | – |
| [12](steps/step-12-paamindelser.md) | Automatiske påmindelser 48 og 24 timer før | Vagttilmeldingssystem | web | Planlagt | Step 10, Step 11 | – |
| [13](steps/step-13-admin-dashboard.md) | Admin-dashboard og deltagerliste | Vagttilmeldingssystem | web | Planlagt | Step 12 | – |
| [14](steps/step-14-mobil-og-tilgaengelighed.md) | Mobil, design og tilgængelighed (gennemgang) | Vagttilmeldingssystem | web | Planlagt | Step 13, Step 18 | – |
| [15](steps/step-15-sikkerhed-og-persondata.md) | Sikkerhed og persondata (gennemgang) | Vagttilmeldingssystem | web | Planlagt | Step 14 | – |
| [16](steps/step-16-docker.md) | Forberedelse til Docker | Vagttilmeldingssystem | løsningen, web | Planlagt | Step 15 | – |
| [17](steps/step-17-samlet-test-og-kravgennemgang.md) | Samlet test og kravgennemgang | Vagttilmeldingssystem | web | Planlagt | Step 16 | – |
| [18](steps/step-18-webmail.md) | Webmail: systemets postkasse | Vagttilmeldingssystem | web | Planlagt | Step 11, Step 13 | – |
