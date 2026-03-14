using System.Linq.Expressions;
using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface ICompanyRepository : IRepository<Company>
{
    public Task<Company?> GetByOwnerIdAsync(
        Guid ownerId,
        bool asNoTracking = false,
        bool includeVenues = false,
        CancellationToken ct = default);
}