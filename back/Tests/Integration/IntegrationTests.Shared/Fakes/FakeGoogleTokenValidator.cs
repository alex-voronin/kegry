using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SecurityModule.Infrastructure.Service;

namespace IntegrationTests.Shared.Fakes;

public class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    public Task<ExternalUserPayload> ValidateAsync(string token, string clientId)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var email = jwt.Claims.First(x => x.Type == "email").Value;
        return Task.FromResult(new ExternalUserPayload(Email: email));
    }

    public static string CreateToken(string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("email", email)]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(new byte[32]), // Not verified by mock validator
                SecurityAlgorithms.HmacSha256Signature
            )
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}