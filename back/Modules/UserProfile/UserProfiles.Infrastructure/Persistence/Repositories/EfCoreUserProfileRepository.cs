using Shared.Infrastructure.Persistence;
using UserProfiles.Domain.Aggregates;

namespace UserProfiles.Infrastructure.Persistence.Repositories;

public class EfCoreUserProfileRepository : Repository<UserProfilesContext, UserProfile>, IUserProfileRepository
{
    public EfCoreUserProfileRepository(UserProfilesContext context) : base(context)
    {
    }
}