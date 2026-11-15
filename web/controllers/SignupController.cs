using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.Enrollment.Dtos;
using web.Managers.Enrollment.Interfaces;
using web.ViewModels.Signup;

namespace web.Controllers;

/// <summary>"Ledige vagter": de åbne vagter og den samlede tilmelding (først-til-mølle, alt eller intet).</summary>
public class SignupController(IEnrollmentManager enrollmentManager) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid? eventId, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        var overview = await enrollmentManager.GetAvailableAsync(userId, cancellationToken);
        return View(overview.ToIndexSignupViewModel(eventId, selected: []));
    }

    [HttpPost]
    public async Task<IActionResult> Enroll(IndexSignupViewModel model, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        var result = await enrollmentManager.EnrollAsync(userId, model.ShiftIds, cancellationToken);
        if (result.Status == EnrollStatus.Success)
        {
            TempData["Success"] = result.CreatedShiftIds.Count == 1
                ? "Du er tilmeldt 1 vagt. Den står nu under Mine vagter."
                : $"Du er tilmeldt {result.CreatedShiftIds.Count} vagter. De står nu under Mine vagter.";
            return RedirectToAction(nameof(MyShiftsController.Index), "MyShifts");
        }

        // Vis siden igen med friske tal. Ingen af vagterne er tildelt (alt eller intet).
        var fresh = await enrollmentManager.GetAvailableAsync(userId, cancellationToken);
        var failed = result.Failures.Select(x => x.ShiftId).ToHashSet();
        var keep = model.ShiftIds.Where(id => !failed.Contains(id)).ToList();

        foreach (var message in SignupViewModelMapper.ErrorMessages(result, fresh))
        {
            ModelState.AddModelError(string.Empty, message);
        }

        return View(nameof(Index), fresh.ToIndexSignupViewModel(model.EventFilter, keep));
    }
}
