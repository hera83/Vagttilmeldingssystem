using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.Assignments.Dtos;
using web.Managers.Assignments.Interfaces;
using web.Managers.Planning.Dtos;
using web.Managers.Planning.Interfaces;
using web.ViewModels.Shifts;

namespace web.Controllers;

[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class ShiftsController(IPlanningManager planningManager, IAssignmentManager assignmentManager) : Controller
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
            switch (result)
            {
                case PlanningResult.Success:
                    TempData["Success"] = $"Vagten {model.Title} er gemt.";
                    return RedirectToAction(nameof(EventsController.Details), "Events", new { id = editor.Event.Id });
                case PlanningResult.ShiftNotFound:
                    return NotFound();
                case PlanningResult.CapacityBelowTaken:
                    ModelState.AddModelError(nameof(model.Capacity), "Der er allerede flere personer på vagten end det nye antal pladser. Fjern nogen under Tildel først.");
                    break;
                default:
                    ModelState.AddModelError(nameof(model.Date), ShiftsViewModelMapper.OutsidePeriodMessage(editor.Event));
                    break;
            }
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

        if (result == PlanningResult.ShiftHasAssignments)
        {
            TempData["Error"] = $"Vagten {editor.Shift.Title} har personer på og kan ikke slettes. Fjern dem under Tildel først.";
        }
        else
        {
            TempData["Success"] = $"Vagten {editor.Shift.Title} er slettet.";
        }

        return RedirectToAction(nameof(EventsController.Details), "Events", new { id = editor.Event.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Assign(Guid id, string? q, CancellationToken cancellationToken)
    {
        var overview = await assignmentManager.GetAssignOverviewAsync(id, q, cancellationToken);
        if (overview is null)
        {
            return NotFound();
        }

        return View(overview.ToAssignShiftsViewModel(q, UserPrincipalFactory.GetUserId(User)));
    }

    [HttpPost]
    public async Task<IActionResult> Assign(Guid id, Guid userId, string? q, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } adminId)
        {
            return Challenge();
        }

        var result = await assignmentManager.AssignAsync(id, userId, adminId, cancellationToken);
        switch (result)
        {
            case AssignUserResult.ShiftNotFound:
                return NotFound();
            case AssignUserResult.Success:
                TempData["Success"] = "Brugeren er sat på vagten.";
                break;
            default:
                TempData["Error"] = ShiftsViewModelMapper.AssignErrorMessage(result);
                break;
        }

        return RedirectToAction(nameof(Assign), new { id, q });
    }

    [HttpPost]
    public async Task<IActionResult> Unassign(Guid id, Guid assignmentId, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } adminId)
        {
            return Challenge();
        }

        var result = await assignmentManager.UnassignAsync(id, assignmentId, adminId, cancellationToken);
        if (result == UnassignResult.NotFound)
        {
            return NotFound();
        }

        TempData["Success"] = "Brugeren er fjernet fra vagten. Pladsen er ledig igen.";
        return RedirectToAction(nameof(Assign), new { id });
    }
}
