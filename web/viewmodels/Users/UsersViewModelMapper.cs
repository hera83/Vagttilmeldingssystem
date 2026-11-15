using web.Extensions;
using web.Managers.Members.Dtos;
using web.Repositories.Users.Dtos;

namespace web.ViewModels.Users;

public static class UsersViewModelMapper
{
    public static IndexUsersViewModel ToIndexUsersViewModel(this List<MemberDto> members, Guid? currentUserId) => new()
    {
        Users = members.Select(x => new UserListItemViewModel
        {
            Id = x.User.Id,
            UserName = x.User.UserName,
            DisplayName = x.User.DisplayName,
            IsAdmin = x.User.IsAdmin,
            IsActive = x.User.IsActive,
            HasCompleteProfile = x.HasCompleteProfile,
            LastLoginText = x.User.LastLoginAt is null ? "Aldrig" : x.User.LastLoginAt.Value.ToDanishShortDateTimeText(),
            IsCurrentUser = x.User.Id == currentUserId
        }).ToList()
    };

    public static ProfileUsersViewModel ToProfileUsersViewModel(this MemberDetailsDto member) => new()
    {
        Id = member.User.Id,
        UserName = member.User.UserName,
        DisplayName = member.User.DisplayName,
        HasProfile = member.Profile is not null,
        IsComplete = member.Profile?.IsComplete == true,
        FullName = member.Profile?.FullName ?? string.Empty,
        Address = member.Profile?.Address ?? string.Empty,
        Phone = member.Profile?.Phone ?? string.Empty,
        ShirtSize = member.Profile?.ShirtSize ?? string.Empty,
        DrinkTickets = member.Profile?.DrinkTickets ?? 0,
        TivoliSheets = member.Profile?.TivoliSheets ?? 0,
        ChildName = member.Profile?.ChildName ?? string.Empty,
        ChildClass = member.Profile?.ChildClass ?? string.Empty,
        UpdatedText = member.Profile is null ? string.Empty : member.Profile.UpdatedAt.ToDanishDateTimeText()
    };

    public static CreateUserDto ToCreateUserDto(this CreateUsersViewModel model) => new()
    {
        UserName = model.UserName,
        DisplayName = model.DisplayName,
        Password = model.Password,
        IsAdmin = model.IsAdmin
    };

    public static EditUsersViewModel ToEditUsersViewModel(this UserDto user, Guid? currentUserId) => new()
    {
        UserName = user.UserName,
        DisplayName = user.DisplayName,
        IsAdmin = user.IsAdmin,
        IsActive = user.IsActive,
        IsCurrentUser = user.Id == currentUserId
    };

    public static UpdateUserDto ToUpdateUserDto(this EditUsersViewModel model) => new()
    {
        DisplayName = model.DisplayName,
        IsAdmin = model.IsAdmin,
        IsActive = model.IsActive
    };

    public static ResetPasswordUsersViewModel ToResetPasswordUsersViewModel(this UserDto user) => new()
    {
        UserName = user.UserName,
        DisplayName = user.DisplayName
    };
}
