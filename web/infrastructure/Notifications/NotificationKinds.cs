namespace web.Infrastructure.Notifications;

/// <summary>Typerne af beskeder i køen (Notifications.Kind). Bruges af flere managers.</summary>
public static class NotificationKinds
{
    public const string ShiftsOpened = "ShiftsOpened";
    public const string Test = "Test";
    public const string Invitation = "Invitation";
    public const string PasswordReset = "PasswordReset";
    public const string EnrollmentConfirmation = "EnrollmentConfirmation";
    public const string Reminder48 = "Reminder48";
    public const string Reminder24 = "Reminder24";

    public static bool IsReminder(string kind) => kind is Reminder48 or Reminder24;

    /// <summary>Beskeder med et engangslink: teksten tømmes, når beskeden er færdigbehandlet.</summary>
    public static bool ContainsSecretLink(string kind) => kind is Invitation or PasswordReset;

    /// <summary>Teksten på knappen i HTML-mailen.</summary>
    public static string ButtonText(string kind) => kind switch
    {
        Invitation => "Vælg adgangskode",
        PasswordReset => "Vælg ny adgangskode",
        EnrollmentConfirmation => "Se mine vagter",
        ShiftsOpened => "Se ledige vagter",
        Reminder48 or Reminder24 => "Se vagten",
        _ => "Åbn Vagttilmelding"
    };
}
