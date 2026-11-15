# Infrastruktur (`infrastructure/`)

**Gælder for:** `infrastructure/` i alle projekter, tværgående kode, som hele projektet bruger.
**Se også:** `authentication.md`, `errorhandling.md`, `logging.md`, `swagger.md`.

## Hvad hører til her?

Hører til her: authentication handlers og policies, global fejlhåndtering, logging-opsætning, OpenAPI-transformere, middleware og andre opsætningsklasser, der kobles på i `Program.cs`.

Hører **ikke** til her:

| Det er … | Så hører det til i |
|---|---|
| Et eksternt system | `services/` |
| Adgang til egen database eller egne filer | `repositories/` |
| Forretningslogik på tværs af repositories/services | `managers/` |
| En ren hjælpefunktion uden afhængigheder | `extensions/` |
| Et baggrundsjob | `bgworkers/` |

## Struktur

```text
<projekt>/infrastructure/
├── Authentication/     ← policies (alle), x-api-key (API-delen), cookie-login (web-delen),
│                         CombinedAuthentication (kombineret)                       (se authentication.md, apikeys.md, cookieauth.md)
├── ErrorHandling/      ← GlobalExceptionHandler                                   (se errorhandling.md)
├── Logging/            ← logging-opsætning, fx LogDatabaseSettings                (se logging.md)
├── OpenApi/            ← OpenAPI-transformere, kun API-delen                      (se swagger.md)
└── Routing/            ← ApiPath, kun kombinerede projekter                       (se authentication.md)
```

## Regler

1. Én undermappe pr. emne i PascalCase med namespace `<projekt>.Infrastructure.<Emne>`.
2. Opret kun `<Emne>/Interfaces/`, hvis emnet faktisk har et interface.
3. Alt kobles på i `Program.cs`.
4. Nyt tværgående emne: ny undermappe `<Emne>/` her. Er emnet stort, så lav også en instruksfil i `.claude/` og importér den fra `CLAUDE.md`.
5. Opdatér tabellen Infrastruktur i `<projekt>/.claude/CLAUDE.md` i samme ændring.

## Kendte afvigelser i koden

Ingen.
