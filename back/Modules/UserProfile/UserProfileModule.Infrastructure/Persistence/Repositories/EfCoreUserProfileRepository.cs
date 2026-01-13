using Shared.Infrastructure.Persistence;
using UserProfileModule.Domain.Aggregates;
using UserProfileModule.Domain.Repositories;
using Wolverine.EntityFrameworkCore;

namespace UserProfileModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserProfileRepository : RepositoryOutbox<UserProfileContext, User>, IUserProfileRepository
{
    public EfCoreUserProfileRepository(IDbContextOutbox<UserProfileContext> contextOutbox) : base(contextOutbox)
    {
    }
}