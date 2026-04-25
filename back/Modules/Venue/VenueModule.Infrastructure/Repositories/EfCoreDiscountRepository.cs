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
            .IgnoreQueryFilters([NamedFilters.Venue.Verified])
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

        // 1. Base filter used for both count and main query
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

        // 2. Get total count of unique venues
        var totalCount = await Context.Discounts
            .Where(baseFilter)
            .GroupBy(d => d.VenueId)
            .CountAsync(ct);

        if (totalCount == 0)
        {
            return ([], 0);
        }

        // 3. Get best discount per venue + pre-calculated distance for global sorting
        var venueData = await Context.Discounts
            .Where(baseFilter)
            .GroupBy(d => d.VenueId)
            .Select(g => new
            {
                BestDiscountId = g
                    .OrderByDescending(x => x.Percent)
                    .ThenBy(x => x.Id)
                    .Select(x => x.Id)
                    .FirstOrDefault(),

                // Pre-calculated values for sorting
                MaxPercent = g.Max(x => x.Percent),
                DistanceMeters = EF.Functions.Distance(
                    g.First().Venue.Coordinates,
                    userLocation,
                    useSpheroid: false),
                CompanyName = g.First().Venue.Company.Name
            })
            .OrderByDescending(x => x.MaxPercent)
            .ThenBy(x => x.DistanceMeters)
            .ThenBy(x => x.CompanyName)
            .Select(x => new
            {
                x.BestDiscountId,
                x.DistanceMeters
            })
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        if (venueData.Count == 0)
        {
            return ([], totalCount);
        }

        var bestDiscountIds = venueData.Select(x => x.BestDiscountId).ToList();

        // 4. Load full discount data (distance already calculated in previous query)
        var rawData = await Context.Discounts
            .AsNoTracking()
            .Include(d => d.Days)
            .Include(d => d.Venue)
            .ThenInclude(v => v.Company)
            .Where(d => bestDiscountIds.Contains(d.Id))
            .ToListAsync(ct); // No need to calculate Distance again

        // 5. Final in-memory sort + attach pre-calculated distance
        //    We use dictionary for fast lookup by DiscountId
        var discountIdToDistance = venueData
            .ToDictionary(x => x.BestDiscountId, x => x.DistanceMeters);

        var items = rawData
            .OrderByDescending(d => d.Percent)
            .ThenBy(d => discountIdToDistance.GetValueOrDefault(d.Id))
            .ThenBy(d => d.Venue.Company.Name)
            .Select(d => new DiscountWithDistance(d, discountIdToDistance.GetValueOrDefault(d.Id)))
            .ToList();

        return (items, totalCount);
    }
}