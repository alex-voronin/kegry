using LocationModule.Domain.Aggregates;
using LocationModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;

namespace LocationModule.Infrastructure.Persistence.Repositories;

internal class EfCoreLocationRepository : Repository<LocationContext, Location>, ILocationRepository
{
    public EfCoreLocationRepository(LocationContext context) : base(context)
    {
    }
}