using EventModule.Domain.Aggregates;
using EventModule.Domain.Enums;
using EventModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Shared.Infrastructure.Persistence;

namespace EventModule.Infrastructure.Persistence.Repositories;

public class EfCoreEventRepository : Repository<EventContext, Event>, IEventRepository
{
    private const double SearchRadiusMeters = 25_000;
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    public EfCoreEventRepository(EventContext context) : base(context)
    {
    }

    public async Task<Event?> GetByIdWithRelationsAsync(Guid id, CancellationToken ct = default)
    {
        return await Context.Events
            .Include(e => e.EventContacts)
            .Include(e => e.EventHobbyLinks)
            .Include(e => e.EventParticipants)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<(EventWithDistance[] Events, int TotalCount)> GetEventsPaginatedAsync(
        double latitude,
        double longitude,
        int offset,
        int limit,
        EventType? type = null,
        Guid? venueId = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        Guid[]? hobbyIds = null,
        CancellationToken ct = default)
    {
        var userPoint = new Point(longitude, latitude) { SRID = 4326 };

        var venueJoinQuery = Context.Events
            .Join(Context.Venues, e => e.VenueId, v => v.Id, (e, v) => new { e, v })
            .Where(x => EF.Functions.IsWithinDistance(
                x.v.Coordinates, userPoint, SearchRadiusMeters, useSpheroid: false));

        if (type.HasValue)
            venueJoinQuery = venueJoinQuery.Where(x => x.e.Type == type.Value);

        if (venueId.HasValue)
            venueJoinQuery = venueJoinQuery.Where(x => x.e.VenueId == venueId.Value);

        if (dateFrom.HasValue)
            venueJoinQuery = venueJoinQuery.Where(x => x.e.StartDateTime >= dateFrom.Value);

        if (dateTo.HasValue)
            venueJoinQuery = venueJoinQuery.Where(x => x.e.StartDateTime <= dateTo.Value);

        if (hobbyIds is { Length: > 0 })
            venueJoinQuery = venueJoinQuery.Where(x => x.e.EventHobbyLinks.Any(h => hobbyIds.Contains(h.HobbyId)));

        var totalCount = await venueJoinQuery.CountAsync(ct);

        var pagedIds = await venueJoinQuery
            .Select(x => new { x.e.Id, Distance = EF.Functions.Distance(x.v.Coordinates, userPoint, useSpheroid: false) })
            .OrderBy(x => x.Distance)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(ct);

        var ids = pagedIds.Select(x => x.Id).ToArray();
        var distanceMap = pagedIds.ToDictionary(x => x.Id, x => x.Distance);

        var events = await Context.Events
            .Include(e => e.EventContacts)
            .Include(e => e.EventHobbyLinks)
            .Include(e => e.EventParticipants)
            .Where(e => ids.Contains(e.Id))
            .ToArrayAsync(ct);

        var result = ids
            .Select(id => new EventWithDistance(events.First(e => e.Id == id), distanceMap[id]))
            .ToArray();

        return (result, totalCount);
    }

    public async Task<Event[]> GetEventsInBoundsAsync(
        double minLat,
        double maxLat,
        double minLng,
        double maxLng,
        EventType? type = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        CancellationToken ct = default)
    {
        var envelope = new Envelope(minLng, maxLng, minLat, maxLat);
        var boundingBox = _geometryFactory.ToGeometry(envelope);

        var query = Context.Events
            .Include(e => e.EventContacts)
            .Include(e => e.EventHobbyLinks)
            .Include(e => e.EventParticipants)
            .Where(e => e.IsVisibleOnMap)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);

        if (dateFrom.HasValue)
            query = query.Where(e => e.StartDateTime >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(e => e.StartDateTime <= dateTo.Value);

        var events = await query.ToArrayAsync(ct);

        var venueIds = events.Select(e => e.VenueId).Distinct().ToArray();
        var venues = await Context.Venues
            .Where(v => venueIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        return events
            .Where(e => venues.TryGetValue(e.VenueId, out var venue) &&
                        venue.Coordinates.Intersects(boundingBox))
            .OrderBy(e => e.StartDateTime)
            .ToArray();
    }
}
