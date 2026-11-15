using Microsoft.AspNetCore.SignalR;

namespace web.Infrastructure.Realtime;

/// <summary>
/// Realtidskanal (/hubs/shifts). Kræver login (fallback-policyen). Klienter kan ikke kalde noget her:
/// serveren sender kun små "noget har ændret sig"-signaler, og siderne henter selv friske data med deres egen adgangskontrol.
/// </summary>
public class ShiftsHub : Hub
{
    public const string Path = "/hubs/shifts";
    public const string AdminGroup = "admins";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole(Authentication.UserClaimTypes.AdminRole) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        }

        await base.OnConnectedAsync();
    }
}
