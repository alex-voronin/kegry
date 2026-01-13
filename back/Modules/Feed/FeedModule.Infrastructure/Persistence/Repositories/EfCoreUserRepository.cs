using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace FeedModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserRepository : Repository<FeedContext, User>, IUserRepository
{
    public EfCoreUserRepository(FeedContext context) : base(context)
    {
    }
}