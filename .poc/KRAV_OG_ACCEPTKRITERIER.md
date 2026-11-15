# Krav og acceptkriterier (sporbar til original PDF)

Dette er en **implementeringsnær fortolkning** af `Projektbeskrivelse_Original.pdf`, ikke en erstatning for PDF'en. Specifikke teknik-/sikkerhedsregler nedenfor er tilføjet som implementeringsbeslutninger, ikke direkte PDF-citater.

## Roller, profil og overblik

| ID | Krav | Hvad der skal demonstreres |
|---|---|---|
| K01 | Én webbaseret løsning til administrator og forældre; PWA på iOS/Android | Desktop, mobil og installerbar PWA åbner samme datasæt |
| K02 | Bruger skal oprette profil før tilmelding | Uden fuldstændig profil er 'Bekræft' låst; server håndhæver også dette |
| K03 | Profil: fulde navn, adresse, telefon, e-mail, trøjestørrelse, væskebilletter, tivolibilletter, barnets navn, klasse | Felter oprettes, lagres, valideres og kan opdateres med nødvendige regler |
| K04 | Tivolibilletter max 2 ark | Input og server tillader kun 0, 1 eller 2 **ark** – ikke '2 billetter' |
| K05 | Roller Administrator og Bruger | Bruger må ikke bruge admin-API eller se andre brugeres private data |
| K06 | Administrator kan oprette/redigere/slette begivenheder og vagter | CRUD, validering og fornuftig håndtering af eksisterende tildelinger |
| K07 | Vagt har tidsrum og maksimal kapacitet | Dato/tid valid, kapacitet >0, besatte pladser overskrider aldrig maksimum |
| K08 | Administrator bestemmer frigivelse | Før frigivelse ses ikke ledige vagter; tydelig bekræftelsesdialog |
| K09 | Kalender/liste viser egne vagter | Også forudtildelte skjulte vagter er synlige **kun for ejeren** |

## Livscyklus og booking

| ID | Krav | Hvad der skal demonstreres |
|---|---|---|
| K10 | Nye vagter er skjulte/kladder | Ingen uautoriseret lækage via API, søgning, SignalR, push eller HTML |
| K11 | Admin kan forhåndstildele skjulte vagter | Både admin og modtager kan se tildeling; andre forældre kan ikke |
| K12 | Frigivelse gør vagter synlige for alle rettidigt | Én bekræftet publicering + samme autoritative resultat på mobil/web; realtidsopdatering |
| K13 | Først-til-mølle og databasebeskyttet kapacitet | Samtidige forsøg kan aldrig overbooke |
| K14 | Bruger kan vælge flere vagter og bekræfte samlet | Ét batchkald, servervalidering, atomisk udfald og forståelig konfliktfejl |
| K15 | Bruger ser egne bookede/tildelte vagter | Kalender/liste med tider, status og oplysninger |
| K16 | Brugere kan ikke selv afmelde/bytte/overdrage | Ingen brugerfunktion og endpoint afviser sådanne handlinger |
| K17 | Administrator kan fjerne en tildeling og genåbne/ophæve en vagt | Kapacitet opdateres, historik bevares og realtime-opdatering sendes |

## Notifikationer

| ID | Krav | Hvad der skal demonstreres |
|---|---|---|
| K18 | Frigivelse giver mulighed for push til iOS og Android | Browser-push/VAPID med tilladelse/abonnement; ingen uønskede test-push til brugere |
| K19 | Automatisk påmindelse 48 timer før vagt | Én varig påmindelsesregistrering pr. tildeling og type; ikke dobbeltudsendelse |
| K20 | Automatisk påmindelse 24 timer før vagt | Samme som K19, inkl. genstart/retries og timezone |
| K21 | Baggrundstjeneste scanner ifølge PDF en gang i timen | Implementeret job, overvågning og dokumenteret præcision/begrænsning |

## Kravkonflikter og præciseringer (bevidst, skal ikke skjules)

1. **PDF: native iOS/Android-apps; brugerens opdaterede valg: PWA.** Implementér én PWA; ingen App Store- eller Play Store-udgivelse. iOS web-push forudsætter understøttet system/browser og typisk tilføjelse til hjemmeskærm.
2. **PDF: job hver time, samtidig 'præcis' 48/24 timer før.** En timelig scanner kan ikke garantere præcise minuttidspunkter, og push kan blive forsinket. POC'en følger standardkravet med **timelig scan**, persisterede due-tidspunkter og idempotens, og dokumenterer faktisk udsendelsesvindue. Hvis finere timing ønskes senere, gør scaninterval konfigurerbart. Beskriv ikke timelig levering som præcis.
3. **Mockups viser ekstra funktioner** (fx rapporter/eksport, to-trinsbekræftelse, beskeder, flere navigationspunkter). Disse er visuelle forslag, **ikke automatiske ekstrakrav**. Kun implementér simple nyttige udvidelser, når de ikke forsinker K01–K21.
4. **Rækkevidde:** Én skole/klasseorganisering er nok til POC; ingen ubegrundet SaaS/multitenancy eller betaling/billetsalg. Billetfelter er profiloplysninger, ikke en webshop.
5. **Først-til-mølle:** serverens transaktionelle rækkefølge er autoritativ; netværksforsinkelser betyder, at man ikke kan garantere absolut 'først trykket på telefonen'.
6. **Tildeling låst:** den er brugerbeskyttet; admin kan stadig fjerne den, som PDF'en kræver.

## Ufravigelige negative tests

- Bruger A kan ikke se Skjult-vagt tildelt bruger B eller opdage den via gættet ID.
- En offentlig begivenhed kan ikke eksponere tilknyttede kladdevagter.
- 50 samtidige tilmeldinger til én ledig plads giver præcis én aktiv booking.
- To parallelle batchbookinger kan ikke give overbookning/deadlock uden korrekt håndtering.
- Et batch med tre valgte vagter, hvor én nu er fuld, giver ingen nye bookinger.
- Efter admin-fjernelse kan den fjernede bruger stadig ikke selv bytte/afmelde andre bookinger.
- Timelig reminder-scan sender ikke samme 24-/48-timers besked to gange ved parallel worker/genstart.
- Push-abonnement uden tilladelse eller på en ikke-understøttet platform håndteres uden app-nedbrud.
