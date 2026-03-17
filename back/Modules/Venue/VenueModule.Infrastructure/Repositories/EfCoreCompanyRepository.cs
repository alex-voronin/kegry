using System.Linq.Expressions;
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

    [Obsolete("Use GetByIdAsync(Guid companyId, Guid ownerId) instead", true)]
    public override Task<Company?> GetByIdAsync<TId>(TId id, 
        bool asNoTracking = false, 
        Expression<Func<Company, object?>>[]? includes = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException("""
                                          Load company via Task<Company?> GetByIdAsync(
                                              Guid companyId,
                                              Guid ownerId,
                                              bool asNoTracking = false,
                                              bool includeVenues = false,
                                              CancellationToken ct = default);
                                          Which load a company only for a particular owner
                                          """);
    }

    public Task<Company[]> GetByOwnerIdAsync(
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
            .OrderBy(c => c.CreatedAt)
            .ToArrayAsync(ct);
    }

    public Task<Company?> GetByIdAsync(
        Guid companyId,
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
            .Where(c => c.Id == companyId && c.OwnerId == ownerId)
            .FirstOrDefaultAsync(ct);
    }
}