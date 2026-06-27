using EventModule.Domain.Aggregates;
using EventModule.Domain.Enums;
using EventModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Shared.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace EventModule.Infrastructure.Persistence.Repositories;

public class EfCoreEventRepository : RepositoryOutbox<EventContext, Event>, IEventRepository
{
    private const double SearchRadiusMeters = 25_000;
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);
    private readonly TimeProvider _timeProvider;

    public EfCoreEventRepository(IDbContextOutbox<EventContext> contextOutbox, TimeProvider timeProvider) :
        base(contextOutbox)
    {
        _timeProvider = timeProvider;
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
        var now = _timeProvider.GetUtcNow();
        var oneMonthFromNow = now.AddDays(30);

        var venueJoinQuery = Context.Events
            .Join(Context.Venues, e => e.VenueId, v => v.Id, (e, v) => new { e, v })
            .Where(x => EF.Functions.IsWithinDistance(
                x.v.Coordinates, userPoint, SearchRadiusMeters, useSpheroid: false))
            .Where(x => x.e.StartDateTime >= now)
            .Where(x => x.e.Type != EventType.Meetup || x.e.StartDateTime <= oneMonthFromNow);

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
            .Select(x => new
            { x.e.Id, Distance = EF.Functions.Distance(x.v.Coordinates, userPoint, useSpheroid: false) })
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

    public async Task<(Event[] Events, int TotalCount)> GetVenueEventsAsync(
        Guid venueId,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var oneMonthFromNow = now.AddDays(30);

        var query = Context.Events
            .Where(e =>
                e.VenueId == venueId &&
                e.IsVisibleOnMap &&
                e.StartDateTime >= now &&
                (e.Type != EventType.Meetup || e.StartDateTime <= oneMonthFromNow));

        var totalCount = await query.CountAsync(ct);

        var events = await query
            .Include(e => e.EventParticipants)
            .OrderBy(e => e.StartDateTime)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(ct);

        return (events, totalCount);
    }

    public async Task<List<(Guid VenueId, int TotalCount, EventWithParticipantStatus[] Events)>> GetEventsInBoundsAsync(
        double minLat,
        double maxLat,
        double minLng,
        double maxLng,
        EventType[]? eventTypes = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        Guid[]? venueIds = null,
        int maxPerVenue = 20,
        Guid? currentUserId = null,
        CancellationToken ct = default)
    {
        if (eventTypes != null && eventTypes.Length == 0)
        {
            return [];
        }

        var envelope = new Envelope(minLng, maxLng, minLat, maxLat);
        var boundingBox = _geometryFactory.ToGeometry(envelope);
        var now = _timeProvider.GetUtcNow();
        var oneMonthFromNow = now.AddDays(30);

        var baseQuery = Context.Events
            .Join(Context.Venues,
                e => e.VenueId,
                v => v.Id,
                (e, v) => new { Event = e, Venue = v })
            .Where(x =>
                x.Event.IsVisibleOnMap &&
                x.Event.StartDateTime >= now &&
                (x.Event.Type != EventType.Meetup || x.Event.StartDateTime <= oneMonthFromNow) &&
                x.Venue.Coordinates.Intersects(boundingBox))
            .Select(x => x.Event);

        if (eventTypes is { Length: > 0 })
            baseQuery = baseQuery.Where(e => eventTypes.Contains(e.Type));
        if (dateFrom.HasValue)
            baseQuery = baseQuery.Where(e => e.StartDateTime >= dateFrom.Value);
        if (dateTo.HasValue)
            baseQuery = baseQuery.Where(e => e.StartDateTime <= dateTo.Value);
        if (venueIds is { Length: > 0 })
            baseQuery = baseQuery.Where(e => venueIds.Contains(e.VenueId));

        var venueCounts = await baseQuery
            .GroupBy(e => e.VenueId)
            .Select(g => new { VenueId = g.Key, TotalCount = g.Count() })
            .ToDictionaryAsync(x => x.VenueId, x => x.TotalCount, ct);

        var allData = await baseQuery
            .OrderBy(e => e.StartDateTime)
            .Take(maxPerVenue)
            .ToArrayAsync(ct);

        var allEventIds = allData.Select(e => e.Id).ToArray();
        var participationStatusMap = currentUserId.HasValue
            ? await Context.EventParticipants
                .Where(ep => ep.UserId == currentUserId.Value && allEventIds.Contains(ep.EventId))
                .Select(ep => new { ep.EventId, ep.Status })
                .ToDictionaryAsync(ep => ep.EventId, ep => ep.Status, ct)
            : new Dictionary<Guid, ParticipantStatus>();

        var result = allData
            .GroupBy(e => e.VenueId)
            .Select(g => (
                VenueId: g.Key,
                TotalCount: venueCounts.GetValueOrDefault(g.Key),
                Events: g
                    .OrderBy(e => e.StartDateTime)
                    .Select(e =>
                        new EventWithParticipantStatus(e,
                            participationStatusMap.ContainsKey(e.Id)
                                ? participationStatusMap.GetValueOrDefault(e.Id)
                                : null)
                    )
                    .ToArray()
            ))
            .ToList();

        return result;
    }

    public async Task<(Event[] Events, int TotalCount)> GetOwnEventsPaginatedAsync(
        Guid userId,
        Guid? venueId = null,
        EventType? type = null,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var weekBefore = now - TimeSpan.FromDays(7);

        var query = Context.Events
            .Include(e => e.EventContacts)
            .Include(e => e.EventHobbyLinks)
            .Include(e => e.EventParticipants)
            .Where(e => e.CreatorId == userId)
            .AsQueryable();

        if (venueId.HasValue)
            query = query.Where(e => e.VenueId == venueId.Value);

        if (type.HasValue)
            query = query.Where(e => e.Type == type.Value);

        if (dateFrom.HasValue)
        {
            query = query.Where(e => e.StartDateTime >= dateFrom.Value);
        }
        else
        {
            query = query.Where(e => e.StartDateTime >= weekBefore);
        }

        if (dateTo.HasValue)
            query = query.Where(e => e.StartDateTime <= dateTo.Value);

        var totalCount = await query.CountAsync(ct);

        var events = await query
            .OrderBy(e => e.StartDateTime)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(ct);

        return (events, totalCount);
    }

    public async Task<(Event[] Events, int TotalCount)> GetParticipatedEventsPaginatedAsync(
        Guid userId,
        int offset = 0,
        int limit = 20,
        CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var query = Context.Events
            .Include(e => e.EventContacts)
            .Include(e => e.EventHobbyLinks)
            .Include(e => e.EventParticipants)
            .Where(e => e.EventParticipants.Any(p =>
                            p.UserId == userId &&
                            (p.Status == ParticipantStatus.Approved || p.Status == ParticipantStatus.Pending)) &&
                        e.StartDateTime >= now &&
                        e.CreatorId != userId);

        var totalCount = await query.CountAsync(ct);

        var events = await query
            .OrderBy(e => e.StartDateTime)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(ct);

        return (events, totalCount);
    }

    public async Task<string[]> GetCoverImageKeysForInactiveEventsAsync(
        DateTimeOffset threshold, CancellationToken ct = default)
    {
        return await Context.Events
            .Where(e => e.StartDateTime < threshold && !e.IsDeleted)
            .Where(e => e.CoverImageS3Key != null)
            .Select(e => e.CoverImageS3Key!)
            .ToArrayAsync(ct);
    }

    public async Task SoftDeleteInactiveEventsAsync(DateTimeOffset threshold, CancellationToken ct = default)
    {
        await Context.Events
            .Where(e => e.StartDateTime < threshold && !e.IsDeleted)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.IsDeleted, true), ct);
    }

    public async Task HardDeleteExpiredEventsAsync(DateTimeOffset threshold, CancellationToken ct = default)
    {
        await Context.Events
            .IgnoreQueryFilters()
            .Where(e => e.StartDateTime < threshold)
            .ExecuteDeleteAsync(ct);
    }
}