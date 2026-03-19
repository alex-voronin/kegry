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

    Task<Venue?> GetByIdWithLyaltylevelsAndDiscounts(Guid id, CancellationToken ct);

    Task<(Venue? venue, Visit? lastVisit, UserLoyalty? userLoyalty)> GetVenueWithLastVisitsAndUserLoyalty(
        Guid venueId, Guid userId, CancellationToken ct);
}