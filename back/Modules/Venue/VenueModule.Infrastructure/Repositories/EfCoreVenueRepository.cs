using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Repositories;
using VenueModule.Domain.ValueObjects;
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