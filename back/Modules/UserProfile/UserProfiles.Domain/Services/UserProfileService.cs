using SharedCore.Services;
using SharedCore.Services.LifeTimes;
using UserProfiles.Domain.Aggregates;

namespace UserProfiles.Domain.Services;

public interface IUserProfileService
{
    public Task<UserProfile> CreateUserProfileAsync();
}

internal class RefreshTokenService : DomainService<IUserProfileRepository>, IUserProfileService, IScoped
{

    //
    // public async Task<RefreshToken> CreateRefreshTokenAsync(
    //     User user, 
    //     Guid jti, 
    //     int durationInDays, 
    //     int maxSimultaneousLoggedInDevices)
    // {
    //     var existingToken = await Repository.GetByJtiAsync(jti.ToString());
    //     if (existingToken != null)
    //     {
    //         throw SecurityExceptions.AccessTokenAlreadyExists(user.Email!);
    //     }
    //
    //     var tokensCount = await Repository.TokensCountAsync(user.Id);
    //     if (tokensCount >= maxSimultaneousLoggedInDevices)
    //     {
    //         throw SecurityExceptions.AccessTokensLimitPerUserIsReached(user.Email!);
    //     }
    //
    //     var token = GenerateToken();
    //     return new RefreshToken(
    //         userId: user.Id,
    //         token: token,
    //         jti: jti.ToString(),
    //         expires: DateTime.UtcNow.AddDays(durationInDays)
    //     );
    // }
    
    public RefreshTokenService(IUserProfileRepository repository) : base(repository)
    {
    }

    public Task<UserProfile> CreateUserProfileAsync()
    {
        throw new NotImplementedException();
    }
}