using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public interface IDiscountRepository : IRepository<Discount>
{
    Task<List<Discount>> GetByVenueIdAsync(Guid venueId, CancellationToken ct = default);

    Task<(List<Discount> Discounts, int TotalCount)> GetAvailableForUserAsync(
        Guid userId,
        double latitude,
        double longitude,
        DayOfWeek dayOfWeek,
        int offset,
        int limit,
        CancellationToken ct = default);
}
