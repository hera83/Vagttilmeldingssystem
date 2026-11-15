using Microsoft.AspNetCore.SignalR;

namespace web.Infrastructure.Realtime;

/// <summary>
/// Beskeder til klienterne (kun id'er, aldrig titler eller persondata):
///   shiftsOpened   { eventId }            alle, når vagter er åbnet
///   shiftsChanged  { eventId, shiftIds }  alle, kun for åbne vagter (pladser taget/ledige, tid ændret)
///   eventChanged   { eventId }            kun administratorer, også for kladder
///   myShiftsChanged {}                    kun de berørte brugere
/// </summary>
public class ShiftUpdatesNotifier(IHubContext<ShiftsHub> hubContext, ILogger<ShiftUpdatesNotifier> logger) : IShiftUpdatesNotifier
{
    public async Task PublishAsync(ShiftUpdate update, CancellationToken cancellationToken = default)
    {
        try
        {
            if (update.IsPublic && update.Opened)
            {
                await hubContext.Clients.All.SendAsync("shiftsOpened", new { eventId = update.EventId }, cancellationToken);
            }
            else if (update.IsPublic)
            {
                await hubContext.Clients.All.SendAsync("shiftsChanged", new { eventId = update.EventId, shiftIds = update.ShiftIds }, cancellationToken);
            }

            await hubContext.Clients.Group(ShiftsHub.AdminGroup).SendAsync("eventChanged", new { eventId = update.EventId }, cancellationToken);

            if (update.AffectedUserIds.Count > 0)
            {
                await hubContext.Clients.Users(update.AffectedUserIds.Select(x => x.ToString()).ToList())
                    .SendAsync("myShiftsChanged", new { }, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Realtid er "best effort": siderne henter friske data ved genforbindelse og genindlæsning.
            logger.LogWarning(ex, "Realtime update for event {EventId} could not be sent", update.EventId);
        }
    }
}
