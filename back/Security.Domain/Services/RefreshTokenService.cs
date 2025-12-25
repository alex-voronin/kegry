using Security.Domain.Exceptions;
using SharedCore.Services;
using Security.Domain.Aggregates.RefreshToken;
using Security.Domain.Aggregates.User;

namespace Security.Domain.Services;

public interface IRefreshTokenService
{
    public Task<RefreshToken> CreateRefreshTokenAsync(User user, Guid jti);
}

public class RefreshTokenService : DomainService<IRefreshTokenRepository>, IRefreshTokenService
{
    private const int DefaultAccessTokenDurationInDays = 180;
    private const int MaxSimultaneousLoggedInDevices = 10; 
    
    public RefreshTokenService(IRefreshTokenRepository repository) : base(repository)
    {
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(User user, Guid jti)
    {
        var existingToken = await Repository.GetByJtiAsync(jti.ToString());
        if (existingToken != null)
        {
            throw SecurityExceptions.AccessTokenAlreadyExists(user.Email!);
        }
        
        var tokensCount = await Repository.TokensCountAsync(user.Id);
        if (tokensCount >= MaxSimultaneousLoggedInDevices)
        {
            throw SecurityExceptions.AccessTokensLimitPerUserIsReached(user.Email!);
        }
        
        var token = GenerateToken();
        return new RefreshToken(
            userId: user.Id,
            token: token,
            jti: jti.ToString(),
            expires: DateTime.UtcNow.AddDays(DefaultAccessTokenDurationInDays)
            );
    }

    private string GenerateToken()
    {
        var guid = Guid.NewGuid();
        return Convert.ToBase64String(guid.ToByteArray());
    }
}