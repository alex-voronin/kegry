using EventModule.Domain.Aggregates;
using EventModule.Domain.Enums;
using SharedCore.Interfaces;

namespace EventModule.Domain.Repositories;

public record EventWithDistance(Event Event, double DistanceMeters);

public record EventWithIsJoined(Event Event, bool IsJoined);

public interface IEventRepository : IRepository<Event>
{
    Task<Event?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default);

    Task<(EventWithDistance[] Events, int TotalCount)> GetEventsPaginatedAsync(
        double latitude,
        double longitude,
        int offset,
        int limit,
        EventType? type = null,
        Guid? venueId = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        Guid[]? hobbyIds = null,
        CancellationToken ct = default);

    Task<(Event[] Events, int TotalCount)> GetVenueEventsAsync(
        Guid venueId,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default);

    Task<List<(Guid VenueId, int TotalCount, EventWithIsJoined[] Events)>> GetEventsInBoundsAsync(
        double minLat,
        double maxLat,
        double minLng,
        double maxLng,
        EventType? type = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        int maxPerVenue = 20,
        Guid? currentUserId = null,
        CancellationToken ct = default);

    Task<(Event[] Events, int TotalCount)> GetOwnEventsPaginatedAsync(
        Guid userId,
        Guid? venueId = null,
        EventType? type = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default);
}
