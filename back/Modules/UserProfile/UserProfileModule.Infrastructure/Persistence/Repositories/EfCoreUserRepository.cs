using Shared.Infrastructure.Persistence;
using UserProfileModule.Domain.Aggregates;
using UserProfileModule.Domain.Repositories;
using Wolverine.EntityFrameworkCore;

namespace UserProfileModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserRepository : RepositoryOutbox<UserProfileContext, User>, IUserRepository
{
    public EfCoreUserRepository(IDbContextOutbox<UserProfileContext> contextOutbox) : base(contextOutbox)
    {
    }
}