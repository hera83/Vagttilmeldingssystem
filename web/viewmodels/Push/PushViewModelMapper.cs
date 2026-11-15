using web.Managers.Notifications.Dtos;

namespace web.ViewModels.Push;

public static class PushViewModelMapper
{
    public static ConfigPushViewModel ToConfigPushViewModel(this PushSettingsDto settings) => new()
    {
        Enabled = settings.Enabled,
        PublicKey = settings.PublicKey
    };

    public static SubscribePushDto ToSubscribePushDto(this SubscribePushViewModel model) => new()
    {
        Endpoint = model.Endpoint.Trim(),
        P256dh = model.Keys.P256dh.Trim(),
        Auth = model.Keys.Auth.Trim()
    };
}
