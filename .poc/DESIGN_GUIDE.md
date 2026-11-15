# Designguide – klassisk enterprise UI og PWA

Brug mockups som **visuelle referencebilleder**, ikke som database- eller rettighedsspecifikation. Bevar ens logotype, farvepalette, navigationsmønstre, knapper, typografi og statuskort gennem hele applikationen.

| PNG i `mockups/` | Skærm | Primær implementering |
|---|---|---|
| [01_forside.png](mockups/01_forside.png) | Forside | STEP_16; forklar PWA og login |
| [02_admin_dashboard.png](mockups/02_admin_dashboard.png) | Admin dashboard | STEP_14 |
| [03_admin_begivenheder_og_vagter.png](mockups/03_admin_begivenheder_og_vagter.png) | Event/vagtoversigt, frigivelsesdialog | STEP_05, 06, 08 |
| [04_admin_opret_vagt.png](mockups/04_admin_opret_vagt.png) | Opret vagt og forhåndstildeling | STEP_06, 07 |
| [05_bruger_ledige_vagter.png](mockups/05_bruger_ledige_vagter.png) | Ledige vagter og multivalg | STEP_10 |
| [06_bruger_min_kalender.png](mockups/06_bruger_min_kalender.png) | Mine vagter + mobil PWA | STEP_11, 12 |
| [07_bruger_min_profil.png](mockups/07_bruger_min_profil.png) | Profil | STEP_04, 16 |

## Design tokens (vejledende)

- Brand: primær blå `#1764B8`, mørk tekst `#13243D`, baggrund `#F6F9FD`, kort `#FFFFFF`, kant `#DFE6EF`, blå svag `#EAF2FD`.
- Status: kladde/skjult neutrale grå/lilla, åbne vagter grøn, advarsel rav/gul, fejl rød. Farve må aldrig være eneste statusindikator.
- Desktop: venstremenu til admin, enkelt header. Mobil: bundnavigation eller tydelig menu til bruger, store klikmål.
- Komponenter: kort, tabel (desktop), kortliste (mobil), dialoger, valgchips, statusbadges, formularvalidering, toast/inline feedback.
- Anvend kontrast, fokusmarkering, label/input-sammenhæng og dansk dato/tid (24 timer, `Europe/Copenhagen`).
- Billedernes eksempler på årstal/personer er **dummydata** og må ikke blandes ind i rigtig funktionalitet.

## Kritisk designregel

En skjult vagt kan være synlig i **den tildelte brugers personlige kalender**. Det betyder ikke, at den må optræde på forsiden eller i listen over **ledige vagter**. Admin-nav og bruger-nav skal afspejle faktiske rettigheder; nogle mockups blander marketingmenu og indlogget menu, men implementeringen skal være logisk.
