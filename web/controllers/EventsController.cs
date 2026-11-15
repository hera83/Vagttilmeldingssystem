using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Data.Models;
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
public class EventsController(IEventRepository eventRepository, IPlanningManager planningManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q, bool archived, CancellationToken cancellationToken)
    {
        var events = await eventRepository.SearchAsync(EventsViewModelMapper.ToEventSearchDto(q, archived), cancellationToken);
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
