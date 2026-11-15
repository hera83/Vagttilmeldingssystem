using web.Extensions;
using web.Repositories.Users.Dtos;

namespace web.ViewModels.Users;

public static class UsersViewModelMapper
{
    public static IndexUsersViewModel ToIndexUsersViewModel(this List<UserDto> users, Guid? currentUserId) => new()
    {
        Users = users.Select(x => new UserListItemViewModel
        {
            Id = x.Id,
            UserName = x.UserName,
            DisplayName = x.DisplayName,
            IsAdmin = x.IsAdmin,
            IsActive = x.IsActive,
            LastLoginText = x.LastLoginAt is null ? "Aldrig" : x.LastLoginAt.Value.ToDanishDateTimeText(),
            IsCurrentUser = x.Id == currentUserId
        }).ToList()
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
