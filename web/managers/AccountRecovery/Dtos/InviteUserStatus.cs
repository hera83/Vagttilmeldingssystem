namespace web.Managers.AccountRecovery.Dtos;

public enum InviteUserStatus
{
    Success,
    UserNameTaken,

    /// <summary>Mail (Mail:*) eller App:Domain er ikke sat op, så der kan ikke sendes et link.</summary>
    MailNotConfigured
}
