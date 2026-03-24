using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreVenueRepository : Repository<VenueContext, Venue>, IVenueRepository
{
    private const double SearchRadiusMeters = 200_000;

    public EfCoreVenueRepository(VenueContext context) : base(context)
    {
    }

    public Task<Venue?> GetByIdWithLoyaltyLevelsAndDiscountsAysnc(Guid id, CancellationToken ct)
    {
        return Context.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Include(v => v.Company)
            .ThenInclude(c => c.LoyaltyLevels)
            .Include(v => v.Discounts)
            .AsNoTracking()
            .Where(v => v.Id == id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(Venue? venue, Visit? lastVisit, UserLoyalty? userLoyalty)>
        GetVenueWithLastVisitsAndUserLoyaltyAsync(Guid qrCode, Guid userId, CancellationToken ct)
    {
        var res = await Context.Venues
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
            .Include(v => v.Company)
            .ThenInclude(c => c.LoyaltyLevels)
            .AsNoTracking()
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

    public async Task<(IReadOnlyList<Venue> venues, int TotalCount)> GetWithPromosInRadiusAsync(
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
                v.Coordinates, userLocation, radiusMeters, useSpheroid: false));

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .AsNoTracking()
            .Include(v => v.Promos)
            .Include(v => v.Company)
            .OrderByDescending(v => v.Promos.Count)
            .ThenBy(v => v.Company.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(VenueWithDistance[], int TotalCount)> GetVenuesPaginatedAsync(double latitude, double longitude,
        int offset, int limit, CancellationToken ct)
    {
        var userCoordinates = new Point(longitude, latitude) { SRID = 4326 };

        var query = Context.Venues
            .Include(v => v.Company)
            .AsNoTracking()
            .Where(v => EF.Functions.IsWithinDistance(
                geometry: v.Coordinates,
                anotherGeometry: userCoordinates,
                distance: SearchRadiusMeters,
                useSpheroid: false)
            );

        var totalCount = await query.CountAsync(ct);

        var venues = await query
            .Select(v =>
                new VenueWithDistance(
                    Venue: v,
                    DistanceMeters: EF.Functions
                        .Distance(v.Coordinates, userCoordinates, useSpheroid: false)
                )
            )
            .OrderBy(v => v.DistanceMeters)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync(ct);

        return (venues, totalCount);
    }
}