# Logs (repository)

**Formål:** Read-only søgning i Serilogs `log.db` (tabellen `Logs`).
**Brug det når:** opgaven skal vise eller søge i appens egne logs.
**Brug det ikke til:** at skrive logs (brug `ILogger<T>`) eller til forretningsdata i `app.db`.
**Tabeller/filer:** `app_dbs/log.db`, tabel `Logs` (oprettes af Serilog-sinken). Læses med ADO.NET, ikke EF Core.

## Metoder
| Metode | Beskrivelse |
|---|---|
| `SearchAsync(SearchLogsDto search)` | Filter på level, tekst i beskeden og UTC-tidsinterval, med paging (højst 500 pr. side). Nyeste først. |

## Faldgruber
- Webprojektet har ingen side, der bruger repositoriet endnu. Det er registreret som standardkomponent (`logging.md`).
- Skriv aldrig i `log.db`. Kun Serilog og `LogCleanupWorker` må.
