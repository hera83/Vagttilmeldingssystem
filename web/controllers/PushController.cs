using Microsoft.AspNetCore.Mvc;
using web.Infrastructure.Authentication;
using web.Managers.Notifications.Dtos;
using web.Managers.Notifications.Interfaces;
using web.ViewModels.Push;

namespace web.Controllers;

/// <summary>
/// Push-abonnementer for den indloggede brugers egne enheder. Kaldes med fetch fra wwwroot/js/push.js (panelet på Min profil),
/// så actions svarer med JSON eller statuskoder i stedet for et view. Anti-forgery-tokenet sendes i headeren RequestVerificationToken.
/// </summary>
public class PushController(INotificationManager notificationManager) : Controller
{
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Config(CancellationToken cancellationToken) =>
        Json((await notificationManager.GetPushSettingsAsync(cancellationToken)).ToConfigPushViewModel());

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] SubscribePushViewModel model, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        return await notificationManager.SubscribeAsync(userId, model.ToSubscribePushDto(), cancellationToken) switch
        {
            SubscribePushResult.Success => NoContent(),
            SubscribePushResult.PushDisabled => Conflict(),
            _ => BadRequest()
        };
    }

    [HttpPost]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribePushViewModel model, CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest();
        }

        // Idempotent: findes abonnementet ikke (eller er det en andens), sker der intet.
        await notificationManager.UnsubscribeAsync(userId, model.Endpoint.Trim(), cancellationToken);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> Test(CancellationToken cancellationToken)
    {
        if (UserPrincipalFactory.GetUserId(User) is not { } userId)
        {
            return Challenge();
        }

        return await notificationManager.QueueTestAsync(userId, cancellationToken) switch
        {
            QueueTestResult.Queued => NoContent(),
            QueueTestResult.NoDevices => NotFound(),
            _ => Conflict()
        };
    }
}
