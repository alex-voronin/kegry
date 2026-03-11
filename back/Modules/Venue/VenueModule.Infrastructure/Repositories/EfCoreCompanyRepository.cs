using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.ValueObjects;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreCompanyRepository : Repository<VenueContext, Company>, ICompanyRepository
{
    public EfCoreCompanyRepository(VenueContext context) : base(context)
    {
    }

    public async Task<(Venue[], int TotalCount)> GetVenuesPaginatedAsync(int offset, int limit, CancellationToken ct)
    {
        var query = Context.Venues
            .Include(v => v.Company)
            .AsNoTracking();

        var totalCount = await query.CountAsync(ct);

        var venues = await query
            .OrderByDescending(v => v.Company.Name)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(ct);

        return (venues, totalCount);
    }
}