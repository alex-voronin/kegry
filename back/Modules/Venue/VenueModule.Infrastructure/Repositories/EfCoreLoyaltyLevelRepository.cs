using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreLoyaltyLevelRepository : Repository<VenueContext, LoyaltyLevel>, ILoyaltyLevelRepository
{
    public EfCoreLoyaltyLevelRepository(VenueContext context) : base(context)
    {
    }

    public Task<List<LoyaltyLevel>> GetByVenueIdAsync(Guid venueId, CancellationToken ct = default)
    {
        return Context.LoyaltyLevels
            .AsNoTracking()
            .Where(l => l.VenueId == venueId)
            .OrderBy(l => l.Id)
            .ToListAsync(ct);
    }

    public async Task DeleteAllForVenueAsync(Guid venueId, CancellationToken ct = default)
    {
        var levels = await Context.LoyaltyLevels
            .Where(l => l.VenueId == venueId)
            .ToListAsync(ct);

        Context.LoyaltyLevels.RemoveRange(levels);
    }
}
