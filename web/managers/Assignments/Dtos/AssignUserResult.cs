namespace web.Managers.Assignments.Dtos;

public enum AssignUserResult
{
    Success,
    ShiftNotFound,
    UserNotFound,
    UserInactive,
    AlreadyAssigned,
    Full
}
