using Google.Apis.Auth;
using SharedCore.Services.LifeTimes;

namespace SecurityModule.Infrastructure.Service;

public record ExternalUserPayload(string Email);

public interface IGoogleTokenValidator
{
    public Task<ExternalUserPayload> ValidateAsync(string token, string clientId);
}

public class GoogleTokenValidator : IGoogleTokenValidator, ISingleton
{
    public async Task<ExternalUserPayload> ValidateAsync(string token, string clientId)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [clientId]
        };

        var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);
        return new ExternalUserPayload(Email: payload.Email);
    }
}