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

    Task<Venue?> GetByIdWithAllRelationsAsync(Guid id, CancellationToken ct);

    Task<Venue?> GetByQrCodeAsync(Guid qrCode, CancellationToken ct);

    Task<(Venue? venue, Visit? lastVisit, UserLoyalty? userLoyalty)> GetVenueWithLastVisitsAndUserLoyaltyAsync(
        Guid qrCode, Guid userId, CancellationToken ct);

    Task<(IReadOnlyList<VenueWithDistance> venues, int TotalCount)> GetVenuesWithPromosInRadiusAsync(
        double latitude,
        double longitude,
        int offset,
        int limit,
        int radiusMeters = 25_000,
        CancellationToken ct = default);

    Task<Venue[]> GetByUserIdAsync(Guid userId, CancellationToken ct);

    Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken ct);

    Task<Venue[]> GetVenuesInBoundsAsync(
        double minLat, double maxLat,
        double minLng, double maxLng,
        int limit,
        CancellationToken ct);
}