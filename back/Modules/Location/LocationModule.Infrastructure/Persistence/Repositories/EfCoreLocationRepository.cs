using LocationModule.Domain.Aggregates;
using Shared.Infrastructure.Persistence;

namespace LocationModule.Infrastructure.Persistence.Repositories;

internal class EfCoreLocationRepository : Repository<LocationContext, Location>, ILocationRepository
{
    public EfCoreLocationRepository(LocationContext context) : base(context)
    {
    }
}