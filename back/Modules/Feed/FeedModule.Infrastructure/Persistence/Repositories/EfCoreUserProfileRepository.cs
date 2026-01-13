using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace FeedModule.Infrastructure.Persistence.Repositories;

internal class EfCoreUserProfileRepository : Repository<FeedContext, User>, IUserProfileRepository
{
    public EfCoreUserProfileRepository(FeedContext context) : base(context)
    {
    }
}