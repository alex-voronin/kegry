using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface ILoyaltyLevelRepository : IRepository<LoyaltyLevel>
{
    Task<List<LoyaltyLevel>> GetByCompanyIdAsync(Guid venueId, CancellationToken ct = default);
}
