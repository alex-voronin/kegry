using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface ILoyaltyLevelRepository : IRepository<LoyaltyLevel>
{
    Task<List<LoyaltyLevel>> GetByVenueIdAsync(Guid venueId, CancellationToken ct = default);
    Task DeleteAllForVenueAsync(Guid venueId, CancellationToken ct = default);
}
