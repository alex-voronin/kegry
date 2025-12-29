using Shared.Infrastructure.Persistence;
using UserProfileModule.Domain.Aggregates;

namespace UserProfileModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserProfileRepository : Repository<UserProfileContext, UserProfile>, IUserProfileRepository
{
    public EfCoreUserProfileRepository(UserProfileContext context) : base(context)
    {
    }
}