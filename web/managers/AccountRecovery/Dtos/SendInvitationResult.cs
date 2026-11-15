namespace web.Managers.AccountRecovery.Dtos;

public enum SendInvitationResult
{
    Sent,
    NotFound,
    Inactive,
    MailNotConfigured
}
