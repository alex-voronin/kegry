using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreDiscountRepository : Repository<VenueContext, Discount>, IDiscountRepository
{
    public EfCoreDiscountRepository(VenueContext context) : base(context)
    {
    }

    public Task<List<Discount>> GetByVenueIdAsync(Guid venueId, CancellationToken ct = default)
    {
        return Context.Discounts
            .AsNoTracking()
            .Where(d => d.VenueId == venueId)
            .ToListAsync(ct);
    }
}
