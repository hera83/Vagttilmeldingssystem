using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using web.Repositories.Users.Dtos;

namespace web.Infrastructure.Authentication;

public static class UserPrincipalFactory
{
    public static ClaimsPrincipal Create(UserDto user)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(UserClaimTypes.DisplayName, user.DisplayName),
            new(UserClaimTypes.SecurityStamp, user.SecurityStamp.ToString())
        ];

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, UserClaimTypes.AdminRole));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}
