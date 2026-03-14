using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface IDiscountRepository : IRepository<Discount>
{
    Task<List<Discount>> GetByVenueIdAsync(Guid venueId, CancellationToken ct = default);
}
