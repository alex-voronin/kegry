using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreDiscountRepository : Repository<VenueContext, Discount>, IDiscountRepository
{
    // GetAvailableForUserAsync is a quite heavy query so only 25 km radius
    private const double RadiusMeters = 25_000.0;

    public EfCoreDiscountRepository(VenueContext context) : base(context)
    {
    }

    public Task<List<Discount>> GetByVenueIdAsync(Guid venueId, CancellationToken ct = default)
    {
        return Context.Discounts
            .Include(d => d.Days)
            .AsNoTracking()
            .Where(d => d.VenueId == venueId)
            .ToListAsync(ct);
    }

    public async Task<(List<Discount> Discounts, int TotalCount)> GetAvailableForUserAsync(
        Guid userId,
        double latitude,
        double longitude,
        DayOfWeek dayOfWeek,
        int offset,
        int limit,
        CancellationToken ct = default)
    {
        var userLocation = new Point(longitude, latitude) { SRID = 4326 };

        var baseQuery = Context.Discounts
            // 1. Venue must be within 100 km of user
            .Where(d => EF.Functions.IsWithinDistance(
                d.Venue.Coordinates, userLocation, RadiusMeters, useSpheroid: false))
            // 2. Must be active on the given day of week
            .Where(d => d.Days.Any(day => day.DayOfWeek == dayOfWeek))
            // 3. Loyalty level check - user's cached loyalty level for this venue is >= the discount's required level.
            .Where(d =>
                d.LoyaltyLevelId == 0 ||
                Context.UserLoyalties
                    .Any(l => l.UserId == userId &&
                              l.CompanyId == d.CompanyId &&
                              l.LoyaltyLevelId >= d.LoyaltyLevelId));

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .AsNoTracking()
            .Include(d => d.Days)
            .Include(d => d.Venue)
            .ThenInclude(v => v.Company)
            .OrderByDescending(d => d.Percent)
            .ThenBy(d => d.Venue.Company.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}