using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SharedCore.Exceptions;

namespace SharedCore.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetId(this ClaimsPrincipal user)
    {
        var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            throw CommonExceptions.InternalError("User id is not found");
        }

        return userId;
    }

    public static string GetEmail(this ClaimsPrincipal user)
    {
        var userEmail = user.FindFirstValue(JwtRegisteredClaimNames.Name);
        if (string.IsNullOrEmpty(userEmail))
        {
            throw CommonExceptions.InternalError("Email is not found");
        }

        return userEmail;
    }
}