using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface IVisitRepository : IRepository<Visit>
{
    Task<Visit?> GetLastVisitAsync(Guid userId, Guid venueId, CancellationToken ct = default);
    Task<int> GetVisitCountAsync(Guid userId, Guid venueId, CancellationToken ct = default);
}