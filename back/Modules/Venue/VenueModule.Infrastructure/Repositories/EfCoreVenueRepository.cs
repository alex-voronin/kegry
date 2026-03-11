using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.ValueObjects;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreVenueRepository : Repository<VenueContext, Venue>, IVenueRepository
{
    public EfCoreVenueRepository(VenueContext context) : base(context)
    {
    }
}