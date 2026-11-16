using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using web.Data.Models;
using web.Extensions;
using web.Managers.Members.Dtos;
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

    private static readonly JsonSerializerOptions ExportJson = new()
    {
        WriteIndented = true,
        // Filen hentes som download og vises aldrig som HTML, så æ, ø og å må stå som de er.
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public static ExportProfileViewModel ToExportProfileViewModel(this MemberDataExportDto dto) => new()
    {
        Hentet = dto.ExportedAtUtc.ToDanishDateTimeText(),
        Konto = new ExportAccountViewModel
        {
            Email = dto.User.UserName,
            Navn = dto.User.DisplayName,
            Administrator = dto.User.IsAdmin,
            Aktiv = dto.User.IsActive,
            Oprettet = dto.User.CreatedAt.ToDanishDateTimeText(),
            SidsteLogin = dto.User.LastLoginAt?.ToDanishDateTimeText()
        },
        Profil = dto.Profile is null ? null : new ExportPersonalDataViewModel
        {
            FuldeNavn = dto.Profile.FullName,
            Adresse = dto.Profile.Address,
            Telefon = dto.Profile.Phone,
            Troejestoerrelse = dto.Profile.ShirtSize,
            Vaeskebilletter = dto.Profile.DrinkTickets,
            Tivoliark = dto.Profile.TivoliSheets,
            BarnetsNavn = dto.Profile.ChildName,
            Klasse = dto.Profile.ChildClass
        },
        EmailBeskeder = dto.EmailNotifications,
        Vagter = dto.Assignments.Select(x => new ExportShiftViewModel
        {
            Begivenhed = x.EventName,
            Vagt = x.ShiftTitle,
            Tidspunkt = $"{x.StartUtc.ToDanishDateText()} kl. {DateTimeExtensions.ToDanishTimeRangeText(x.StartUtc, x.EndUtc)}",
            Tilmeldt = x.Source == AssignmentSource.Self ? "Selv" : "Af administrator",
            TilmeldtDen = x.AssignedAt.ToDanishDateTimeText(),
            FjernetDen = x.CancelledAt?.ToDanishDateTimeText()
        }).ToList(),
        EnhederMedPush = dto.Devices.Select(x => new ExportDeviceViewModel
        {
            PushTjeneste = x.PushService,
            SlaaetTil = x.CreatedAt.ToDanishDateTimeText(),
            SidstLeveret = x.LastSuccessAt?.ToDanishDateTimeText()
        }).ToList(),
        Beskeder = dto.Notifications.Select(x => new ExportMessageViewModel
        {
            Type = x.Kind,
            Kanal = x.Channel == NotificationChannel.Email ? "E-mail" : "Push",
            Status = x.Status.ToString(),
            Emne = x.Title,
            Oprettet = x.CreatedAt.ToDanishDateTimeText(),
            Sendt = x.SentAt?.ToDanishDateTimeText()
        }).ToList()
    };

    public static byte[] ToJsonBytes(this ExportProfileViewModel model) => JsonSerializer.SerializeToUtf8Bytes(model, ExportJson);

    public static string ToExportFileName(this MemberDataExportDto dto, bool own) =>
        own ? $"mine-data-{dto.ExportedAtUtc.ToDanishTime():yyyy-MM-dd}.json"
            : $"data-{dto.User.DisplayName.ToFileNameSlug()}-{dto.ExportedAtUtc.ToDanishTime():yyyy-MM-dd}.json";
}
