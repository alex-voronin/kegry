using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCorePromoRepository : Repository<VenueContext, Promo>, IPromoRepository
{
    public EfCorePromoRepository(VenueContext context) : base(context)
    {
    }
}