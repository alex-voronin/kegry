using SecurityModule.Domain.Aggregates;
using SharedCore.Services;
using SecurityModule.Domain.Exceptions;
using SecurityModule.Domain.Repositories;
using SharedCore.Services.LifeTimes;

namespace SecurityModule.Domain.Services;

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
    private readonly TimeProvider _timeProvider;
    
    public RefreshTokenService(IRefreshTokenRepository repository, TimeProvider timeProvider) : base(repository)
    {
        _timeProvider = timeProvider;
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
            createdAt: _timeProvider.GetUtcNow().UtcDateTime,
            expiresAt: _timeProvider.GetUtcNow().UtcDateTime.AddDays(durationInDays)
        );
    }

    private static string GenerateToken()
    {
        var guid = Guid.CreateVersion7();
        return Convert.ToBase64String(guid.ToByteArray());
    }
}