using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Data.Models;
using web.Extensions;
using web.Infrastructure.Authentication;
using web.Managers.Planning.Dtos;
using web.Managers.Planning.Interfaces;
using web.Repositories.Events.Interfaces;
using web.ViewModels.Events;

namespace web.Controllers;

/// <summary>
/// Begivenheder (admin). Actions med kun begivenheden bruger repositoriet direkte. Details og Delete går via
/// PlanningManager, fordi de også bruger vagterne (hybrid-reglen pr. action).
/// </summary>
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class EventsController(IEventRepository eventRepository, IPlanningManager planningManager, ILogger<EventsController> logger) : Controller
{
    /// <summary>Deltagerlisten som CSV (persondata, kun admin). Hændelsen logges uden persondata.</summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Participants(Guid id, CancellationToken cancellationToken)
    {
        var list = await planningManager.GetParticipantsAsync(id, cancellationToken);
        if (list is null)
        {
            return NotFound();
        }

        logger.LogInformation("Participant list for event {EventId} exported by user {UserId} ({Rows} rows)",
            id, UserPrincipalFactory.GetUserId(User), list.Rows.Count);
        return File(CsvHelper.ToUtf8WithBom(list.ToParticipantsCsv()), "text/csv; charset=utf-8", list.ToParticipantsFileName(DateTime.UtcNow));
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, bool archived, CancellationToken cancellationToken)
    {
        var events = await planningManager.SearchEventsAsync(EventsViewModelMapper.ToEventSearchDto(q, archived), cancellationToken);
        var counts = await eventRepository.GetCountsAsync(cancellationToken);
        return View(events.ToIndexEventsViewModel(counts, q, archived));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, ShiftStatus? status, CancellationToken cancellationToken)
    {
        var overview = await planningManager.GetEventOverviewAsync(id, status, cancellationToken);
        if (overview is null)
        {
            return NotFound();
        }

        return View(overview.ToDetailsEventsViewModel(status));
    }

    [HttpGet]
    public async Task<IActionResult> Open(Guid id, CancellationToken cancellationToken)
    {
        var overview = await planningManager.GetEventOverviewAsync(id, cancellationToken: cancellationToken);
        if (overview is null)
        {
            return NotFound();
        }

        if (overview.Event.IsArchived || overview.DraftCount == 0)
        {
            TempData["Error"] = overview.Event.IsArchived
                ? "Begivenheden er arkiveret. Gør den aktiv igen, før du åbner for tilmelding."
                : "Der er ingen kladder at åbne.";
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(overview.ToOpenEventsViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Open(Guid id, OpenEventsViewModel model, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } adminId)
        {
            return Challenge();
        }

        if (model.ShiftIds.Count == 0)
        {
            var overview = await planningManager.GetEventOverviewAsync(id, cancellationToken: cancellationToken);
            if (overview is null)
            {
                return NotFound();
            }

            // Fejlen står i opsummeringen: "All" ville indsætte et skjult <li style> og bryde CSP (step 15).
            ModelState.AddModelError(string.Empty, "Vælg mindst én vagt, der skal åbnes.");
            return View(overview.ToOpenEventsViewModel(model.ShiftIds));
        }

        var result = await planningManager.OpenShiftsAsync(id, model.ShiftIds, adminId, cancellationToken);
        switch (result.Result)
        {
            case PlanningResult.EventNotFound:
                return NotFound();
            case PlanningResult.EventArchived:
                TempData["Error"] = "Begivenheden er arkiveret. Gør den aktiv igen, før du åbner for tilmelding.";
                break;
            default:
                if (result.OpenedShiftIds.Count == 0)
                {
                    TempData["Error"] = "Der var ingen kladder at åbne. De valgte vagter er allerede åbne.";
                }
                else
                {
                    TempData["Success"] = result.OpenedShiftIds.Count == 1
                        ? "1 vagt er nu åben for tilmelding."
                        : $"{result.OpenedShiftIds.Count} vagter er nu åbne for tilmelding.";
                }

                break;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateEventsViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(CreateEventsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var created = await eventRepository.CreateAsync(model.ToCreateEventDto(), cancellationToken);
        TempData["Success"] = $"Begivenheden {created.Name} er oprettet.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var item = await eventRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return View(item.ToEditEventsViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, EditEventsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Id = id;
            return View(model);
        }

        var updated = await eventRepository.UpdateAsync(id, model.ToUpdateEventDto(), cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        TempData["Success"] = $"Begivenheden {updated.Name} er gemt.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        if (!await eventRepository.SetArchivedAsync(id, archived: true, cancellationToken))
        {
            return NotFound();
        }

        TempData["Success"] = "Begivenheden er arkiveret.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        if (!await eventRepository.SetArchivedAsync(id, archived: false, cancellationToken))
        {
            return NotFound();
        }

        TempData["Success"] = "Begivenheden er aktiv igen.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await planningManager.DeleteEventAsync(id, cancellationToken);
        if (result == PlanningResult.EventNotFound)
        {
            return NotFound();
        }

        if (result == PlanningResult.EventHasShifts)
        {
            TempData["Error"] = "Begivenheden har vagter og kan ikke slettes. Slet vagterne først, eller arkivér begivenheden i stedet.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Begivenheden er slettet.";
        return RedirectToAction(nameof(Index));
    }
}
