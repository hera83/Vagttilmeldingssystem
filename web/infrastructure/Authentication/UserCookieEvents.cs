using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using web.Repositories.Users.Interfaces;

namespace web.Infrastructure.Authentication;

public class UserCookieEvents(IUserRepository userRepository) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userId = context.Principal is null ? null : UserPrincipalFactory.GetUserId(context.Principal);
        var stamp = context.Principal?.FindFirstValue(UserClaimTypes.SecurityStamp);
        var user = userId is null ? null : await userRepository.GetByIdAsync(userId.Value, context.HttpContext.RequestAborted);

        if (user is null || !user.IsActive || user.SecurityStamp.ToString() != stamp)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
