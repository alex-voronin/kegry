using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.ValueObjects;

namespace VenueModule.Domain.Repositories;

public interface ICompanyRepository : IRepository<Company>
{
    Task<(Venue[], int TotalCount)> GetVenuesPaginatedAsync(int offset, int limit, CancellationToken ct);
}