using Security.Domain.Exceptions;
using SharedCore.Services;
using Security.Domain.Aggregates.RefreshToken;
using Security.Domain.Aggregates.User;
using SharedCore.Services.LifeTimes;

namespace Security.Domain.Services;

public interface IRefreshTokenService
{
    public Task<RefreshToken> CreateRefreshTokenAsync(
        User user,
        Guid jti,
        int durationInDays,
        int maxSimultaneousLoggedInDevices);
}

internal class RefreshTokenService : DomainService<IRefreshTokenRepository>, IRefreshTokenService, IScoped
{
    public RefreshTokenService(IRefreshTokenRepository repository) : base(repository)
    {
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(
        User user, 
        Guid jti, 
        int durationInDays, 
        int maxSimultaneousLoggedInDevices)
    {
        var existingToken = await Repository.GetByJtiAsync(jti.ToString());
        if (existingToken != null)
        {
            throw SecurityExceptions.AccessTokenAlreadyExists(user.Email!);
        }

        var tokensCount = await Repository.TokensCountAsync(user.Id);
        if (tokensCount >= maxSimultaneousLoggedInDevices)
        {
            throw SecurityExceptions.AccessTokensLimitPerUserIsReached(user.Email!);
        }

        var token = GenerateToken();
        return new RefreshToken(
            userId: user.Id,
            token: token,
            jti: jti.ToString(),
            expires: DateTime.UtcNow.AddDays(durationInDays)
        );
    }

    private string GenerateToken()
    {
        var guid = Guid.NewGuid();
        return Convert.ToBase64String(guid.ToByteArray());
    }
}