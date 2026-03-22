using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.Repositories;

public record VenueWithDistance(Venue Venue, double DistanceMeters);

public interface IVenueRepository : IRepository<Venue>
{
    Task<(VenueWithDistance[], int TotalCount)> GetVenuesPaginatedAsync(
        double latitude,
        double longitude,
        int offset,
        int limit,
        CancellationToken ct);

    Task<Venue?> GetByIdWithLoyaltyLevelsAndDiscountsAysnc(Guid id, CancellationToken ct);

    Task<(Venue? venue, Visit? lastVisit, UserLoyalty? userLoyalty)> GetVenueWithLastVisitsAndUserLoyaltyAsync(
        Guid venueId, Guid userId, CancellationToken ct);

    Task<(IReadOnlyList<Venue> venues, int TotalCount)> GetWithPromosInRadiusAsync(
        double latitude,
        double longitude,
        int offset,
        int limit,
        int radiusMeters = 25_000,
        CancellationToken ct = default);

    Task<Venue[]> GetByUserIdAsync(Guid userId, CancellationToken ct);
}