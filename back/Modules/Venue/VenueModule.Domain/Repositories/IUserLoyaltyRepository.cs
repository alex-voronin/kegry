using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface IUserLoyaltyRepository : IRepository<UserLoyalty>
{
    Task<UserLoyalty?> GetByUserAndVenueId(Guid userId, Guid venueId, CancellationToken ct = default);
}