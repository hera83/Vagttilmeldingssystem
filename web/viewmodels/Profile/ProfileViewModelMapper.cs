using web.Repositories.Profiles.Dtos;

namespace web.ViewModels.Profile;

public static class ProfileViewModelMapper
{
    public static IndexProfileViewModel ToIndexProfileViewModel(this ProfileDto? profile, string email)
    {
        var model = profile is null
            ? new IndexProfileViewModel()
            : new IndexProfileViewModel
            {
                FullName = profile.FullName,
                Address = profile.Address,
                Phone = profile.Phone,
                ShirtSize = profile.ShirtSize,
                DrinkTickets = profile.DrinkTickets,
                TivoliSheets = profile.TivoliSheets,
                ChildName = profile.ChildName,
                ChildClass = profile.ChildClass,
                EmailNotifications = profile.EmailNotifications
            };

        model.FillDisplayFields(email, profile?.IsComplete == true);
        return model;
    }

    public static void FillDisplayFields(this IndexProfileViewModel model, string email, bool isComplete)
    {
        model.Email = email;
        model.IsComplete = isComplete;
        model.ShirtSizeOptions = ShirtSizes.All;
    }

    public static SaveProfileDto ToSaveProfileDto(this IndexProfileViewModel model) => new()
    {
        FullName = model.FullName,
        Address = model.Address,
        Phone = model.Phone,
        ShirtSize = model.ShirtSize,
        DrinkTickets = model.DrinkTickets ?? 0,
        TivoliSheets = model.TivoliSheets ?? 0,
        ChildName = model.ChildName,
        ChildClass = model.ChildClass
    };
}
