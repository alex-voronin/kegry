
using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface ICompanyRepository : IRepository<Company>
{
    public Task<Company[]> GetByOwnerIdAsync(
        Guid ownerId,
        bool asNoTracking = false,
        bool includeVenues = false,
        CancellationToken ct = default);

    public Task<int> CountByOwnerIdAsync(Guid ownerId, CancellationToken ct = default);

    public Task<Company?> GetByIdAsync(
        Guid companyId,
        Guid ownerId,
        bool asNoTracking = false,
        bool includeVenues = false,
        bool includeLoyaltyLevels = false,
        bool ignoreVerifiedFilter = false,
        CancellationToken ct = default);
}