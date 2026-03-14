using SharedCore.Interfaces;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.ValueObjects;

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

    Task<(Venue? venue, Visit? lastVisit, UserLoyalty? userLoyalty)> GetVenueWithLastVisitsAndUserLoyalty(
        Guid venueId, Guid userId, CancellationToken ct);
}