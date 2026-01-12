using Shared.Infrastructure.Persistence;
using UserProfileModule.Domain.Aggregates;
using UserProfileModule.Domain.Repostories;
using Wolverine.EntityFrameworkCore;

namespace UserProfileModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserProfileRepository : RepositoryOutbox<UserProfileContext, UserProfile>, IUserProfileRepository
{
    public EfCoreUserProfileRepository(IDbContextOutbox<UserProfileContext> contextOutbox) : base(contextOutbox)
    {
    }
}