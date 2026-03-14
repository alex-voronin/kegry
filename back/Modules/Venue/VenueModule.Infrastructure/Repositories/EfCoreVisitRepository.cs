using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreVisitRepository : Repository<VenueContext, Visit>, IVisitRepository
{
    public EfCoreVisitRepository(VenueContext context) : base(context)
    {
    }

    public Task<int> GetVisitCountAsync(Guid userId, Guid venueId, CancellationToken ct = default)
    {
        return Context.Visits
            .Where(v => v.UserId == userId && v.VenueId == venueId)
            .CountAsync(ct);
    }
}