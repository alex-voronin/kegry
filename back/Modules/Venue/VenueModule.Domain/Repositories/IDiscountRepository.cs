using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public record DiscountWithDistance(Discount Discount, double DistanceMeters);

public interface IDiscountRepository : IRepository<Discount>
{
    Task<List<Discount>> GetByVenueIdAsync(Guid venueId, CancellationToken ct = default);

    Task<(List<DiscountWithDistance> Discounts, int TotalCount)> GetAvailableDiscountsInRadiusAsync(
        Guid userId,
        double latitude,
        double longitude,
        DayOfWeek dayOfWeek,
        int offset,
        int limit,
        int radiusMeters = 25_000,
        CancellationToken ct = default);
}
