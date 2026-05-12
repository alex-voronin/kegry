using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreVenueRepository : Repository<VenueContext, Venue>, IVenueRepository
{
    private const double SearchRadiusMeters = 25_000;
    private readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

    public EfCoreVenueRepository(VenueContext context) : base(context)
    {
    }

    public Task<Venue?> GetByIdWithAllRelationsAsync(Guid id, CancellationToken ct)
    {
        return Context.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Include(v => v.Company)
            .ThenInclude(c => c.LoyaltyLevels)
            .Include(v => v.Discounts)
            .ThenInclude(d => d.Days)
            .Include(v => v.Discounts)
            .ThenInclude(d => d.LoyaltyLevel)
            .Include(v => v.Promos)
            .AsNoTracking()
            .Where(v => v.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public Task<Venue?> GetByQrCodeAsync(Guid qrCode, CancellationToken ct)
    {
        return Context.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Where(v => v.QrCode == qrCode)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(Venue? venue, Visit? lastVisit, UserLoyalty? userLoyalty)>
        GetVenueWithLastVisitsAndUserLoyaltyAsync(Guid qrCode, Guid userId, CancellationToken ct)
    {
        var res = await Context.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Include(v => v.Company)
            .ThenInclude(c => c.LoyaltyLevels)
            .Where(v => v.QrCode == qrCode)
            .Select(v => new
            {
                Venue = v,
                LastVisit = Context.Visits
                    .Where(vis => vis.UserId == userId && vis.VenueId == v.Id)
                    .OrderByDescending(vis => vis.CheckedInAt)
                    .FirstOrDefault(),
                VisitCount = Context.UserLoyalties
                    .Where(l => l.UserId == userId && l.CompanyId == v.CompanyId)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);
        return res == null ? (null, null, null) : (res.Venue, res.LastVisit, res.VisitCount);
    }

    public Task<Venue[]> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return Context.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Where(v => v.Company.OwnerId == userId)
            .OrderBy(v => v.Id)
            .ToArrayAsync(ct);
    }

    // B2C venues listings must respect NamedFilters.Venue.Verified query filter

    public async Task<(IReadOnlyList<VenueWithDistance> venues, int TotalCount)> GetVenuesWithPromosInRadiusAsync(
        double latitude,
        double longitude,
        int offset,
        int limit,
        int radiusMeters = 25_000,
        CancellationToken ct = default)
    {
        var userLocation = new Point(longitude, latitude) { SRID = 4326 };

        var baseQuery = Context.Venues
            .Where(v => EF.Functions.IsWithinDistance(
                v.Coordinates, userLocation, radiusMeters, useSpheroid: false))
            .Where(v => v.Promos.Count > 0);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .AsNoTracking()
            .Include(v => v.Promos)
            .Include(v => v.Company)
            .Select(v => new
            {
                Venue = v,
                DistanceMeters = EF.Functions.Distance(
                    v.Coordinates,
                    userLocation,
                    useSpheroid: false)
            })
            .OrderByDescending(t => t.Venue.Promos.Count)
            .ThenBy(t => t.DistanceMeters)
            .ThenBy(t => t.Venue.Company.Name)
            .Skip(offset)
            .Take(limit)
            .Select(x => new VenueWithDistance(
                Venue: x.Venue,
                DistanceMeters: x.DistanceMeters))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(VenueWithDistance[], int TotalCount)> GetVenuesPaginatedAsync(
        double latitude,
        double longitude,
        int offset,
        int limit,
        CancellationToken ct)
    {
        var userCoordinates = new Point(longitude, latitude) { SRID = 4326 };

        var baseQuery = Context.Venues
            .Include(v => v.Company)
            .AsNoTracking()
            .Where(v => EF.Functions.IsWithinDistance(
                v.Coordinates,
                userCoordinates,
                SearchRadiusMeters,
                useSpheroid: false));

        var totalCount = await baseQuery.CountAsync(ct);

        var venues = await baseQuery
            .Select(v => new
            {
                Venue = v,
                DistanceMeters = EF.Functions.Distance(
                    v.Coordinates,
                    userCoordinates,
                    useSpheroid: false)
            })
            .OrderBy(x => x.DistanceMeters)
            .Skip(offset)
            .Take(limit)
            .Select(x => new VenueWithDistance(
                Venue: x.Venue,
                DistanceMeters: x.DistanceMeters))
            .ToArrayAsync(ct);

        return (venues, totalCount);
    }

    public Task<int> CountByCompanyIdAsync(Guid companyId, CancellationToken ct)
    {
        return Context.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .CountAsync(v => v.CompanyId == companyId, ct);
    }

    public async Task<Venue[]> GetVenuesInBoundsAsync(
        double minLat, double maxLat,
        double minLng, double maxLng,
        int limit,
        CancellationToken ct)
    {
        var envelope = new Envelope(minLng, maxLng, minLat, maxLat);
        var boundingBox = _geometryFactory.ToGeometry(envelope);

        return await Context.Venues
            .Include(v => v.Company)
            .AsNoTracking()
            .Where(v => v.Coordinates.Intersects(boundingBox))
            .OrderBy(v => v.Company.Name)
            .Take(limit)
            .ToArrayAsync(ct);
    }
}