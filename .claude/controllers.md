# Controllers, DTO'er og ViewModels (`controllers/`, `dtos/`, `viewmodels/`)

**Gælder for:** `controllers/`, `dtos/` og `viewmodels/` i alle projekter.
**Se også:** `views.md` (views til web-controllere), `authentication.md` (adgang), `errorhandling.md` (fejlsvar), `managers.md` (hvornår der skal bruges en manager), `repositories.md` og `services.md` (det, controllere kalder).

## To slags controllere

| | API-controller (API-delen) | Web-controller (web-delen) |
|---|---|---|
| Arver | `ControllerBase` | `Controller` |
| Attributter på klassen | `[ApiController]` + `[Route(...)]` | Ingen route. Standard-routen `{controller=Home}/{action=Index}/{id?}` bruges. |
| Route | API: `[Route("[controller]/[action]")]`. Kombineret: `[Route("api/[controller]/[action]")]` | `/<Navn>/<Action>/{id?}` |
| Ligger i | API: `controllers/`. Kombineret: `controllers/Api/` (namespace `<projekt>.Controllers.Api`) | `controllers/` (namespace `<projekt>.Controllers`) |
| Ind og ud | `dtos/<Navn>/`: Request-/ResponseDto | `viewmodels/<Navn>/`: ViewModel |
| Returnerer | `ActionResult<T>` / `IActionResult` med JSON | `IActionResult`: `View(...)`, `RedirectToAction(...)`, `NotFound()` |
| Login | `x-api-key` | Cookie |
| Fejl | ProblemDetails | Fejlside / statusside, valideringsfejl i viewet |

I et kombineret projekt kan en API-controller og en web-controller godt hedde det samme (fx `<Navn>Controller` i både `controllers/` og `controllers/Api/`), fordi de ligger i hver sit namespace.

## Struktur

```text
<projekt>/
├── controllers/
│   ├── <Navn>Controller.cs                   ← web-controller, eller API-controller i et rent API-projekt   (namespace <projekt>.Controllers)
│   └── Api/                                  ← kun kombinerede projekter
│       └── <Navn>Controller.cs               ← API-controller   (namespace <projekt>.Controllers.Api)
├── dtos/                                     ← API-delen
│   └── <Navn>/                               ← samme <Navn> som API-controlleren   (namespace <projekt>.Dtos.<Navn>)
│       ├── <Action><Navn>RequestDto.cs       ← kun når actionen modtager body/query
│       ├── <Action><Navn>ResponseDto.cs      ← kun når actionen returnerer en body
│       ├── <Del>Dto.cs                       ← under-model brugt inde i en DTO
│       └── <Navn>Mapper.cs
└── viewmodels/                               ← web-delen
    └── <Navn>/                               ← samme <Navn> som web-controlleren   (namespace <projekt>.ViewModels.<Navn>)
        ├── <Action><Navn>ViewModel.cs        ← én pr. view
        ├── <Del>ViewModel.cs                 ← under-model, fx en række i en liste
        └── <Navn>ViewModelMapper.cs
```

## Regler for alle controllere

1. Alle controllere hedder `<Navn>Controller`. Brug aldrig minimal APIs eller Razor Pages.
2. `<Navn>` bliver en del af URL'en og navnet på DTO-, ViewModel- og view-mappen. Brug flertal for samlinger af ting (`<Entiteter>Controller`) og ental for enkeltstående funktioner (som `HealthController` og `AccountController`).
3. Afhængigheder injiceres via primary constructor: `public class <Navn>Controller(I<Entitet>Repository <entitet>Repository) : Controller`.
4. Hver async action tager `CancellationToken cancellationToken` som sidste parameter og sender den videre.
5. Dataadgang følger hybrid-reglen i `managers.md`: én afhængighed → kald repository/service direkte, flere → brug en manager. Controllere injicerer aldrig `<Projekt>DbContext` eller `HttpClient`.
6. Ingen try/catch for at forme fejl eller logge. `GlobalExceptionHandler` gør begge dele (se `errorhandling.md`).
7. **Adgang:** alt kræver login (web) eller nøgle (API) uden ekstra kode. `[AllowAnonymous]` må kun bruges bevidst og skrives i projektfilen med begrundelse. Admin-funktioner bruger policies fra `AuthorizationPolicies` (se `authentication.md`).
8. Ny eller ændret controller: opdatér tabellen Controllers og endpoints i `<projekt>/.claude/CLAUDE.md` i samme ændring.

## API-controllere (API-delen)

9. Klassen har altid `[ApiController]` og route-attributten fra tabellen ovenfor. Action-attributter må kun indeholde parametre, aldrig action-navnet igen: `[HttpGet("{id:guid}")]` på `GetById` giver `/<Navn>/GetById/{id}` (eller `/api/<Navn>/GetById/{id}`).
10. **Kombineret:** API-controllere har desuden `[IgnoreAntiforgeryToken]`. Web-delen validerer anti-forgery globalt, og API-kald bruger `x-api-key` i stedet for en cookie, så de har intet token.
11. Returtype `ActionResult<T>`, når der returneres en body, og `IActionResult`, når der ikke gør (fx `NoContent()`).
12. Fejl: `NotFound()`, `BadRequest()`, `Conflict()` osv. Det giver automatisk ProblemDetails.

### DTO'er
13. Alle API-DTO'er ligger i `dtos/<Navn>/`, hvor `<Navn>` er controllerens navn uden `Controller`. Én klasse pr. fil.
14. Hver action har sine **egne** DTO'er. To actions deler aldrig en Request- eller ResponseDto.
15. **RequestDto** findes kun, når actionen modtager en body eller query-parametre. Et enkelt route-id (`{id}`) er bare en metodeparameter. Komplekse RequestDtos på `GET`/`DELETE` skal have `[FromQuery]`.
16. **ResponseDto** findes kun, når actionen returnerer en body. Returnerer en action en liste uden paging, er returtypen `List<<Action><Navn>ResponseDto>`, så hvert element er actionens ResponseDto (fx `ActionResult<List<GetAll<Navn>ResponseDto>>`). Med paging returneres én ResponseDto med `Items` af `<Del>Dto` og felter til paging.
17. Navne: `<Action><Navn>RequestDto` og `<Action><Navn>ResponseDto`, hvor `<Action>` er metodenavnet.
18. Under-modeller, der bruges inde i en DTO (fx elementerne i en liste), hedder `<Del>Dto` og må deles mellem DTO'erne i samme mappe.
19. Validering sker med data-annotations på RequestDtos (`[Required]`, `[StringLength]` …). `[ApiController]` svarer selv 400.
20. Strings får `= string.Empty` og samlinger `= []`. Kun reelt valgfrie felter er nullable.

## Web-controllere (web-delen)

21. Klassen arver `Controller` og har ingen `[ApiController]` og ingen `[Route]`. Standard-routen giver `/<Navn>/<Action>/{id?}`. Et route-id hedder derfor altid `id`.
22. Alle actions har `[HttpGet]` eller `[HttpPost]`. En formular har to actions med samme navn: `[HttpGet] <Action>` viser formularen, og `[HttpPost] <Action>` modtager den.
23. **Post/Redirect/Get:** efter en vellykket POST svares der altid med `RedirectToAction(...)`, aldrig med et view. Så kan brugeren genindlæse siden uden at sende formularen igen.
24. Validering: `if (!ModelState.IsValid) return View(model);`. Forventede fejl (fx "findes allerede") lægges i `ModelState.AddModelError(...)`, og viewet vises igen. Findes en ting ikke, så `return NotFound();`. Det giver statussiden.
25. **Anti-forgery** valideres globalt på alle POST/PUT/DELETE (filteret i `Program.cs`). Brug altid `<form asp-…>` i views, så tokenet kommer med af sig selv. `[IgnoreAntiforgeryToken]` må kun stå på `HomeController.Error`/`Status` (se `views.md`) og på API-controllere.
26. Ændringer sker kun via POST. En GET må aldrig oprette, rette eller slette noget.
27. Et view får altid en ViewModel og aldrig en repository-, manager- eller service-Dto eller en EF-entity.
28. Brug `nameof(...)` i `RedirectToAction`, når det er muligt: `RedirectToAction(nameof(Index))`.

### ViewModels
29. Alle ViewModels ligger i `viewmodels/<Navn>/`, hvor `<Navn>` er web-controllerens navn uden `Controller`. Én klasse pr. fil.
30. Navne: `<Action><Navn>ViewModel`. GET- og POST-actionen med samme navn deler én ViewModel (fx `Create<Navn>ViewModel` til både visning og modtagelse af formularen). Ellers har hver action sin egen.
31. Under-modeller (fx en række i en tabel) hedder `<Del>ViewModel` og må deles i samme mappe.
32. Validering med data-annotations, og altid med `ErrorMessage` og `[Display(Name = "...")]` på projektets UI-sprog, så brugeren aldrig ser .NET's engelske standardtekster.
33. Felter, der kun bruges til at vise noget (fx en liste til en dropdown), fyldes i controlleren igen, når en POST fejler og viewet vises igen.

## Mappers

34. Al mapning mellem API-DTO'er/ViewModels og repositoriers/managers/servicers Dtos ligger i mapperen: `dtos/<Navn>/<Navn>Mapper.cs` (API) og `viewmodels/<Navn>/<Navn>ViewModelMapper.cs` (web), som `public static class` med extension-metoder. Ingen mapning inde i controlleren, heller ikke `new <Dto> { … }` af en repository- eller service-Dto. Har mapningen intet kildeobjekt (fx kun to simple værdier), så er det en almindelig statisk metode i mapperen (`<Navn>Mapper.To<Måltype>(værdi1, værdi2)`).
35. Mapper-metoder hedder `To<Måltype>()`: `<entitet>.ToGetById<Navn>ResponseDto()`, `request.ToCreate<Entitet>Dto()`, `<entitet>.ToEdit<Navn>ViewModel()`, `model.ToUpdate<Entitet>Dto()`.

## Skabelon: API-controller

```csharp
// controllers/<Navn>Controller.cs  (kombineret: controllers/Api/<Navn>Controller.cs)
namespace <projekt>.Controllers;          // kombineret: <projekt>.Controllers.Api

[ApiController]
[Route("[controller]/[action]")]           // kombineret: [Route("api/[controller]/[action]")]
// [Kombineret] [IgnoreAntiforgeryToken]
public class <Navn>Controller(I<Entitet>Repository <entitet>Repository) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetById<Navn>ResponseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var <entitet> = await <entitet>Repository.GetByIdAsync(id, cancellationToken);
        if (<entitet> is null)
        {
            return NotFound();
        }

        return Ok(<entitet>.ToGetById<Navn>ResponseDto());
    }

    [HttpPost]
    public async Task<ActionResult<Create<Navn>ResponseDto>> Create(Create<Navn>RequestDto request, CancellationToken cancellationToken)
    {
        var <entitet> = await <entitet>Repository.CreateAsync(request.ToCreate<Entitet>Dto(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = <entitet>.Id }, <entitet>.ToCreate<Navn>ResponseDto());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await <entitet>Repository.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}

// dtos/<Navn>/<Navn>Mapper.cs
public static class <Navn>Mapper
{
    public static GetById<Navn>ResponseDto ToGetById<Navn>ResponseDto(this <Entitet>Dto dto) => new()
    {
        Id = dto.Id
        // … øvrige felter
    };

    public static Create<Entitet>Dto ToCreate<Entitet>Dto(this Create<Navn>RequestDto request) => new()
    {
        // … felter fra request
    };
}
```

## Skabelon: web-controller

```csharp
// controllers/<Navn>Controller.cs
namespace <projekt>.Controllers;

public class <Navn>Controller(I<Entitet>Repository <entitet>Repository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var <entiteter> = await <entitet>Repository.GetAllAsync(cancellationToken);
        return View(<entiteter>.ToIndex<Navn>ViewModel());
    }

    [HttpGet]
    public IActionResult Create() => View(new Create<Navn>ViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(Create<Navn>ViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await <entitet>Repository.CreateAsync(model.ToCreate<Entitet>Dto(), cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var <entitet> = await <entitet>Repository.GetByIdAsync(id, cancellationToken);
        if (<entitet> is null)
        {
            return NotFound();
        }

        return View(<entitet>.ToEdit<Navn>ViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, Edit<Navn>ViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await <entitet>Repository.UpdateAsync(id, model.ToUpdate<Entitet>Dto(), cancellationToken);
        return updated is null ? NotFound() : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await <entitet>Repository.DeleteAsync(id, cancellationToken) ? RedirectToAction(nameof(Index)) : NotFound();
}

// viewmodels/<Navn>/Create<Navn>ViewModel.cs
namespace <projekt>.ViewModels.<Navn>;

public class Create<Navn>ViewModel
{
    [Required(ErrorMessage = "Skriv et navn.")]
    [StringLength(200, ErrorMessage = "Navnet må højst være {1} tegn.")]
    [Display(Name = "Navn")]
    public string Name { get; set; } = string.Empty;
}

// viewmodels/<Navn>/<Navn>ViewModelMapper.cs
public static class <Navn>ViewModelMapper
{
    public static Index<Navn>ViewModel ToIndex<Navn>ViewModel(this List<<Entitet>Dto> <entiteter>) => new()
    {
        Items = <entiteter>.Select(x => new <Entitet>ListItemViewModel { Id = x.Id /* … */ }).ToList()
    };

    public static Create<Entitet>Dto ToCreate<Entitet>Dto(this Create<Navn>ViewModel model) => new()
    {
        // … felter fra model
    };
}
```

Views til web-controlleren står i `views.md`.

## Kendte afvigelser i koden

Ingen. Bemærk: ændrede routes er breaking changes for API-klienterne og for links til websiderne.
