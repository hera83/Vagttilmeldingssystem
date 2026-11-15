using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Repositories.Assignments.Interfaces;
using web.ViewModels.MyShifts;

namespace web.Controllers;

/// <summary>
/// "Mine vagter": kun den indloggede brugers egne aktive vagter, også dem en administrator har tildelt i det skjulte.
/// Der findes bevidst ingen afmeld-, byt- eller giv videre-handling (krav K16).
/// </summary>
public class MyShiftsController(IAssignmentRepository assignmentRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? month, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        var shifts = await assignmentRepository.GetActiveForUserAsync(userId, cancellationToken);
        return View(shifts.ToIndexMyShiftsViewModel(month, DateTime.UtcNow));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        // Slås op på både bruger og vagt: en andens vagt (eller en kladde, man ikke er på) giver 404.
        var shift = await assignmentRepository.GetActiveForUserAndShiftAsync(userId, id, cancellationToken);
        if (shift is null)
        {
            return NotFound();
        }

        return View(shift.ToDetailsMyShiftsViewModel(DateTime.UtcNow));
    }
}
