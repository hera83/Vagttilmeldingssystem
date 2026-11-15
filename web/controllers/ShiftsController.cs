using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.Planning.Dtos;
using web.Managers.Planning.Interfaces;
using web.ViewModels.Shifts;

namespace web.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ShiftsController(IPlanningManager planningManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(Guid eventId, CancellationToken cancellationToken)
    {
        var item = await planningManager.GetEventAsync(eventId, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        return View(item.ToCreateShiftsViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid eventId, CreateShiftsViewModel model, CancellationToken cancellationToken)
    {
        var item = await planningManager.GetEventAsync(eventId, cancellationToken);
        if (item is null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var result = await planningManager.CreateShiftAsync(eventId, model.ToCreateShiftDto(), cancellationToken);
            if (result == PlanningResult.Success)
            {
                TempData["Success"] = $"Vagten {model.Title} er oprettet som skjult kladde.";
                return RedirectToAction(nameof(EventsController.Details), "Events", new { id = eventId });
            }

            if (result == PlanningResult.EventNotFound)
            {
                return NotFound();
            }

            ModelState.AddModelError(nameof(model.Date), ShiftsViewModelMapper.OutsidePeriodMessage(item));
        }

        model.FillDisplayFields(item);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var editor = await planningManager.GetShiftEditorAsync(id, cancellationToken);
        if (editor is null)
        {
            return NotFound();
        }

        return View(editor.ToEditShiftsViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, EditShiftsViewModel model, CancellationToken cancellationToken)
    {
        var editor = await planningManager.GetShiftEditorAsync(id, cancellationToken);
        if (editor is null)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            var result = await planningManager.UpdateShiftAsync(id, model.ToUpdateShiftDto(), cancellationToken);
            if (result == PlanningResult.Success)
            {
                TempData["Success"] = $"Vagten {model.Title} er gemt.";
                return RedirectToAction(nameof(EventsController.Details), "Events", new { id = editor.Event.Id });
            }

            if (result == PlanningResult.ShiftNotFound)
            {
                return NotFound();
            }

            ModelState.AddModelError(nameof(model.Date), ShiftsViewModelMapper.OutsidePeriodMessage(editor.Event));
        }

        model.FillDisplayFields(editor);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var editor = await planningManager.GetShiftEditorAsync(id, cancellationToken);
        if (editor is null)
        {
            return NotFound();
        }

        var result = await planningManager.DeleteShiftAsync(id, cancellationToken);
        if (result == PlanningResult.ShiftNotFound)
        {
            return NotFound();
        }

        TempData["Success"] = $"Vagten {editor.Shift.Title} er slettet.";
        return RedirectToAction(nameof(EventsController.Details), "Events", new { id = editor.Event.Id });
    }
}
