using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreUserLoyaltyRepository : Repository<VenueContext, UserLoyalty>, IUserLoyaltyRepository
{
    public EfCoreUserLoyaltyRepository(VenueContext context) : base(context)
    {
    }

    public Task<UserLoyalty?> GetByUserAndVenueId(Guid userId, Guid venueId, CancellationToken ct = default)
    {
        return Context.UserLoyalties
            .Where(l => l.UserId == userId && l.VenueId == venueId)
            .FirstOrDefaultAsync(ct);
    }
}