using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SharedCore.Exceptions;

namespace SharedCore.Extensions;

public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        public Guid GetId()
        {
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                throw CommonExceptions.InternalError("User id is not found");
            }

            return userId;
        }

        public string GetEmail()
        {
            var userEmail = user.FindFirstValue(JwtRegisteredClaimNames.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                throw CommonExceptions.InternalError("Email is not found");
            }

            return userEmail;
        }
    }
}