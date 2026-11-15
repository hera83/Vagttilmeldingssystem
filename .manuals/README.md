# `.manuals/`: manualer til projekterne

Her ligger én manual pr. projekt i løsningen. Manualerne beskriver altid, hvordan projekterne virker **nu**: sider, API, administration, konfiguration, drift og fejlfinding. Mappen er en punktum-mappe, så den er gitignored og kun findes lokalt.

## Opbygning

```text
.manuals/
├── README.md            ← denne fil
└── <projekt>/           ← samme navn som projektmappen, fx api eller web
    ├── manual.md        ← manualen
    └── images/          ← billederne til manualen
```

## Sådan holdes de opdateret

- Når et projekt oprettes, oprettes dets manual samtidig.
- Hver ændring i et projekt opdaterer manualen i samme ændring og får en linje i manualens **Ændringslog**.
- Hver plan i `.project/` har "Opdatér manualen" som fast punkt, og et step er ikke færdigt, før manualerne er opdateret.
- Skærmbilleder tages af den kørende app med testdata. Kan Claude ikke tage et billede, står det under **Manglende billeder** nederst i manualen, og du kan selv lægge filen i `images/` med det angivne navn.
- Manualerne indeholder aldrig rigtige nøgler eller adgangskoder, kun navnene på de indstillinger, hvor de sættes.

Reglerne står i `.claude/manuals.md`.

## Manualer

| Projekt | Type | Manual | Sidst opdateret |
|---|---|---|---|
