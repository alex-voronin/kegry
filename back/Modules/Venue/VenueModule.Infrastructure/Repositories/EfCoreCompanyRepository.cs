using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreCompanyRepository : Repository<VenueContext, Company>, ICompanyRepository
{
    public EfCoreCompanyRepository(VenueContext context) : base(context)
    {
    }

    public Task<Company?> GetByOwnerIdAsync(
        Guid ownerId,
        bool asNoTracking = false,
        bool includeVenues = false,
        CancellationToken ct = default)
    {
        IQueryable<Company> query = Context.Companies;

        if (includeVenues)
        {
            query = query.Include(c => c.Venues);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query
            .Where(c => c.OwnerId == ownerId)
            .FirstOrDefaultAsync(ct);
    }
}