using FeedModule.Domain.Aggregates.ReadOnly;
using FeedModule.Domain.Repositories;
using Shared.Infrastructure.Persistence;

namespace FeedModule.Infrastructure.Persistence.Repositories;

internal class EfCoreLocationRepository : Repository<FeedContext, Location>, ILocationRepository
{
    public EfCoreLocationRepository(FeedContext context) : base(context)
    {
    }
}