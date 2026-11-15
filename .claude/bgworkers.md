# Baggrundsjob (`bgworkers/`)

**Gælder for:** `bgworkers/` i alle projekter, alle baggrundsjob (`BackgroundService` registreret med `AddHostedService`).
**Se også:** `managers.md` (hvornår et job skal bruge en manager), `configuration.md` (intervaller), `logging.md`.

## Formål

Alle baggrundsjob har ét fælles hjem, også når et job hører til én bestemt feature (som standardens `LogCleanupWorker`). Det skal være nemt at se, hvad der kører i baggrunden.

## Struktur

```text
<projekt>/bgworkers/           ← små bogstaver som alle topmapper (project.md)
└── <Navn>Worker.cs            ← namespace <projekt>.BgWorkers
```

`<Navn>` beskriver, hvad jobbet gør, fx `<Feature>Polling` eller `<Entiteter>Cleanup`.

## Regler

1. Alle baggrundsjob ligger i `bgworkers/`, aldrig i featurens egen mappe. Namespace: `<projekt>.BgWorkers`.
2. Klassen hedder `<Navn>Worker` og arver `BackgroundService`.
3. Registrér den i `Program.cs` under `// Workers`: `builder.Services.AddHostedService<<Navn>Worker>();`
4. **Scope:** en worker er singleton. Repositories, managers, services og `<Projekt>DbContext` er scoped og må aldrig injiceres direkte i constructoren. Injicér `IServiceScopeFactory`, og opret et nyt scope pr. gennemløb med `await using var scope = scopeFactory.CreateAsyncScope();`.
5. **Fejl:** en uhåndteret exception i `ExecuteAsync` stopper hele appen. Hvert gennemløb pakkes derfor i try/catch, som logger fejlen og fortsætter. `OperationCanceledException` ved nedlukning er forventet og må ikke logges som fejl.
6. Brug `PeriodicTimer` til gentagne job, og send `stoppingToken` videre til alle kald.
7. Intervallet læses fra `<Feature>:WorkerIntervalSeconds` i `appsettings.json` med en standardværdi i koden. Rene vedligeholdelsesjob med et fast interval må bruge en konstant.
8. Hybrid-reglen fra `managers.md` gælder også her: én afhængighed kaldes direkte, flere går via en manager.
9. Opdatér tabellen Workers i `<projekt>/.claude/CLAUDE.md` i samme ændring.

## Skabelon

```csharp
public class <Navn>Worker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<<Navn>Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(configuration.GetValue("<Feature>:WorkerIntervalSeconds", 30));
        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    // Én afhængighed: repository/service direkte. Flere: en manager.
                    var manager = scope.ServiceProvider.GetRequiredService<I<Område>Manager>();
                    await manager.<Handling>Async(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Forventet ved nedlukning.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "{Worker} failed", nameof(<Navn>Worker));
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // WaitForNextTickAsync kaster, når stoppingToken annulleres.
        }
    }
}
```

## Kendte afvigelser i koden

Ingen. Projektets nuværende workers står i `<projekt>/.claude/CLAUDE.md`.
