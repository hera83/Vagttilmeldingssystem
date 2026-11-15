using web.Extensions;
using web.Managers.Enrollment.Dtos;
using web.Repositories.Assignments.Dtos;

namespace web.ViewModels.Signup;

public static class SignupViewModelMapper
{
    public static IndexSignupViewModel ToIndexSignupViewModel(this AvailableOverviewDto overview, Guid? eventFilter, IReadOnlyCollection<Guid> selected)
    {
        var events = overview.Shifts
            .GroupBy(x => x.Shift.EventId)
            .Select(g => new SignupEventCardViewModel
            {
                EventId = g.Key,
                Name = g.First().Shift.EventName,
                Location = g.First().Shift.EventLocation,
                DateText = g.Min(x => x.Shift.StartUtc).ToDanishDateText(),
                FreeShiftCount = g.Count(x => !x.IsMine && x.Free > 0),
                IsSelected = g.Key == eventFilter
            })
            .ToList();

        // Et filter på en begivenhed, der ikke (længere) har åbne vagter, ignoreres.
        var filter = events.Any(x => x.EventId == eventFilter) ? eventFilter : null;

        return new IndexSignupViewModel
        {
            ShiftIds = selected.ToList(),
            EventFilter = filter,
            ProfileComplete = overview.ProfileComplete,
            Events = events,
            Shifts = overview.Shifts
                .Where(x => filter is null || x.Shift.EventId == filter)
                .Select(x => new SignupShiftRowViewModel
                {
                    ShiftId = x.Shift.Id,
                    Title = x.Shift.Title,
                    Description = x.Shift.Description,
                    EventName = x.Shift.EventName,
                    Location = x.Shift.EventLocation,
                    DateText = x.Shift.StartUtc.ToDanishDateText(),
                    TimeText = DateTimeExtensions.ToDanishTimeRangeText(x.Shift.StartUtc, x.Shift.EndUtc),
                    Free = x.Free,
                    Capacity = x.Shift.Capacity,
                    IsMine = x.IsMine,
                    Selected = !x.IsMine && x.Free > 0 && selected.Contains(x.Shift.Id)
                })
                .ToList()
        };
    }

    /// <summary>
    /// Forståelige fejl. Titler vises kun for vagter, brugeren må se (dem i den friske liste over åbne vagter),
    /// så en manipuleret POST med en kladdes id aldrig afslører kladden.
    /// </summary>
    public static IEnumerable<string> ErrorMessages(EnrollResultDto result, AvailableOverviewDto fresh)
    {
        switch (result.Status)
        {
            case EnrollStatus.NothingSelected:
                yield return "Vælg mindst én vagt, før du bekræfter.";
                yield break;
            case EnrollStatus.ProfileIncomplete:
                yield return "Du skal udfylde din profil, før du kan tilmelde dig vagter. Gå til Min profil.";
                yield break;
        }

        yield return "Du er ikke tilmeldt nogen af vagterne, fordi mindst én af dem ikke kunne gennemføres. Tjek dit valg, og bekræft igen.";

        var known = fresh.Shifts.ToDictionary(x => x.Shift.Id);
        var unknown = false;
        foreach (var failure in result.Failures)
        {
            if (!known.TryGetValue(failure.ShiftId, out var shift))
            {
                unknown = true;
                continue;
            }

            var name = $"{shift.Shift.Title} ({shift.Shift.StartUtc.ToDanishDateText()} {DateTimeExtensions.ToDanishTimeRangeText(shift.Shift.StartUtc, shift.Shift.EndUtc)})";
            yield return failure.Reason switch
            {
                AssignFailureReason.Full => $"{name} blev fuld, før din tilmelding blev gennemført.",
                AssignFailureReason.AlreadyAssigned => $"Du er allerede på {name}.",
                _ => $"{name} er ikke længere åben for tilmelding."
            };
        }

        if (unknown)
        {
            yield return "En af de valgte vagter er ikke åben for tilmelding.";
        }
    }
}
