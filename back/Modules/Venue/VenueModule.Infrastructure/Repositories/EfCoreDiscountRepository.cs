using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Shared.Infrastructure.Persistence;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Repositories;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Infrastructure.Repositories;

public class EfCoreDiscountRepository : Repository<VenueContext, Discount>, IDiscountRepository
{
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

    public async Task<(List<DiscountWithDistance> Discounts, int TotalCount)> GetAvailableDiscountsInRadiusAsync(
        Guid userId,
        double latitude,
        double longitude,
        DayOfWeek dayOfWeek,
        int offset,
        int limit,
        int radiusMeters = 25_000,
        CancellationToken ct = default)
    {
        var userLocation = new Point(longitude, latitude) { SRID = 4326 };

        // Filter for all queries
        Expression<Func<Discount, bool>> baseFilter = d =>
            d.Venue.IsVerified &&
            !d.Venue.IsDeleted &&
            d.LoyaltyLevel.IsActive &&
            EF.Functions.IsWithinDistance(d.Venue.Coordinates, userLocation, radiusMeters, useSpheroid: false) &&
            d.Days.Any(day => day.DayOfWeek == dayOfWeek) &&
            (d.LoyaltyLevelId == 0 ||
             Context.UserLoyalties.Any(l =>
                 l.UserId == userId &&
                 l.CompanyId == d.CompanyId &&
                 l.LoyaltyLevelId >= d.LoyaltyLevelId));

        // 2. Get total count
        var totalCount = await Context.Discounts
            .Where(baseFilter)
            .GroupBy(d => d.VenueId)
            .CountAsync(ct);

        if (totalCount == 0)
        {
            return ([], 0);
        }

        // 3. Get ids of the best applicable discounts.
        // (We have to split it to 2 queries as EF Core can't translate GroupBy() +
        // OrderByDescending() + FirstOrDefault() + Includes())
        var bestDiscountIds = await Context.Discounts
            .Where(baseFilter)
            .GroupBy(d => d.VenueId)
            .Select(g => g
                .OrderByDescending(x => x.Percent)
                .ThenBy(x => x.Id) // tie-breaker for stable sorting
                .Select(x => x.Id)
                .FirstOrDefault())
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        if (bestDiscountIds.Count == 0)
        {
            return ([], totalCount);
        }

        // 4. Load actual data
        var rawData = await Context.Discounts
            .AsNoTracking()
            .Include(d => d.Days)
            .Include(d => d.Venue)
            .ThenInclude(v => v.Company)
            .Where(d => bestDiscountIds.Contains(d.Id))
            .Select(d => new
            {
                Discount = d,
                DistanceMeters = EF.Functions.Distance(
                    d.Venue.Coordinates,
                    userLocation,
                    useSpheroid: false)
            })
            .ToListAsync(ct);

        // 5. Sort them in memory
        var items = rawData
            .OrderByDescending(x => x.Discount.Percent)
            .ThenBy(x => x.DistanceMeters)
            .ThenBy(x => x.Discount.Venue.Company.Name)
            .Select(x => new DiscountWithDistance(x.Discount, x.DistanceMeters))
            .ToList();

        return (items, totalCount);
    }
}