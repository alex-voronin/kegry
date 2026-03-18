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

    public Task<List<LoyaltyLevel>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        return Context.LoyaltyLevels
            .AsNoTracking()
            .Where(l => l.CompanyId == companyId)
            .OrderBy(l => l.Id)
            .ToListAsync(ct);
    }
}
