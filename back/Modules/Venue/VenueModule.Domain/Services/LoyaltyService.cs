using GeoTimeZone;
using SharedCore.Geolocation;
using SharedCore.Services.LifeTimes;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;

namespace VenueModule.Domain.Services;

public record LoyaltyLevelWithDiscount(
    int UserLoyaltyLevel,
    int VisitsCount,
    int VisitsToNextLoyaltyLevel,
    int Discount);

public interface ILoyaltyService
{
    Task<(List<Discount> Discounts, int TotalCount)> GetAvailableDiscountsAsync(
        Guid userId,
        double latitude,
        double longitude,
        int offset,
        int limit,
        CancellationToken ct);

    Task<LoyaltyLevelWithDiscount> GetLoyaltyLevelWithDiscountAsync(
        Guid userId,
        Guid venueId,
        CancellationToken ct = default);

    Task CheckInToVenue(
        Guid userId, Guid venueId,
        double userLatitude, double userLongitude,
        CancellationToken ct = default);
}

internal class LoyaltyService : ILoyaltyService, IScoped
{
    internal const double MaxCheckInDistanceMeters = 100.0;
    internal const double CheckInCooldownHours = 12;

    private readonly IVenueRepository _venueRepository;
    private readonly IVisitRepository _visitRepository;
    private readonly IUserLoyaltyRepository _userLoyaltyRepository;
    private readonly IDiscountRepository _discountRepository;

    public LoyaltyService(
        IVenueRepository venueRepository,
        IVisitRepository visitRepository,
        IUserLoyaltyRepository userLoyaltyRepository,
        IDiscountRepository discountRepository)
    {
        _venueRepository = venueRepository;
        _visitRepository = visitRepository;
        _userLoyaltyRepository = userLoyaltyRepository;
        _discountRepository = discountRepository;
    }

    public async Task<(List<Discount> Discounts, int TotalCount)> GetAvailableDiscountsAsync(
        Guid userId,
        double latitude,
        double longitude,
        int offset,
        int limit,
        CancellationToken ct)
    {
        var timezoneId = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var dayOfWeek = userLocalTime.DayOfWeek;
        return await _discountRepository.GetAvailableForUserAsync(
            userId: userId,
            latitude: latitude,
            longitude: longitude,
            dayOfWeek: dayOfWeek,
            offset: offset,
            limit: limit,
            ct: ct);
    }

    public async Task CheckInToVenue(
        Guid userId, Guid venueId,
        double userLatitude, double userLongitude,
        CancellationToken ct = default)
    {
        var transaction = await _venueRepository
            .And(_userLoyaltyRepository)
            .StartTransactionAsync(ct);

        var (venue, lastVisit, userLoyalty) = await _venueRepository.GetVenueWithLastVisitsAndUserLoyalty(
            venueId: venueId,
            userId: userId,
            ct: ct);

        if (userLoyalty == null)
        {
            userLoyalty = new UserLoyalty
            {
                UserId = userId,
                VenueId = venueId,
                LoyaltyLevelId = 0,
                VisitsCount = 0
            };
            await _userLoyaltyRepository.AddAsync(userLoyalty, ct);
        }

        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(venueId);
        }

        var distanceMeters = Haversine.DistanceMeters(
            userLatitude, userLongitude,
            venue.Coordinates.Y, venue.Coordinates.X);

        if (distanceMeters > MaxCheckInDistanceMeters)
        {
            throw VenueExceptions.TooFarFromVenue();
        }

        if (lastVisit != null && lastVisit.CheckedInAt > DateTime.UtcNow - TimeSpan.FromHours(CheckInCooldownHours))
        {
            throw VenueExceptions.AlreadyCheckedInRecently();
        }

        var visit = new Visit
        {
            UserId = userId,
            VenueId = venueId,
            CheckedInAt = DateTimeOffset.UtcNow
        };
        await _visitRepository.AddAsync(visit, ct);

        var newVisitsCount = userLoyalty.VisitsCount + 1;
        var (currentLoyaltyLevelId, _) = GetCurrentLoyaltyLevel(venue.LoyaltyLevels, newVisitsCount);

        userLoyalty.LoyaltyLevelId = currentLoyaltyLevelId;
        userLoyalty.VisitsCount = newVisitsCount;

        await _visitRepository.SaveChangesAsync(ct);
        await _userLoyaltyRepository.SaveChangesAsync(ct);
        await transaction.SaveChangesAsync(ct);
    }

    public async Task<LoyaltyLevelWithDiscount> GetLoyaltyLevelWithDiscountAsync(
        Guid userId,
        Guid venueId,
        CancellationToken ct = default)
    {
        var venue = await _venueRepository.GetByIdAsync(venueId,
            asNoTracking: true,
            includes: [v => v.Discounts, v => v.LoyaltyLevels],
            ct: ct);

        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(venueId);
        }

        var userLoyalty = await _userLoyaltyRepository.GetByUserAndVenueId(userId, venueId, ct);
        var visitsCount = userLoyalty?.VisitsCount ?? 0;

        var (currentLoyaltyLevel, visitsToNextLoyaltyLevel) = GetCurrentLoyaltyLevel(
            venue.LoyaltyLevels, visitsCount);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(venue.TimeZoneId);
        var venueLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var today = venueLocalTime.DayOfWeek;

        var applicableDiscounts = venue.Discounts
            .Where(d => d.Days.Any(day => day.DayOfWeek == today))
            .Where(d => currentLoyaltyLevel >= d.LoyaltyLevelId)
            .ToList();

        var maxDiscount = applicableDiscounts.Count > 0 ? applicableDiscounts.Max(d => d.Percent) : 0;
        return new LoyaltyLevelWithDiscount(currentLoyaltyLevel, visitsCount, visitsToNextLoyaltyLevel, maxDiscount);
    }

    internal static (int currentLoyaltyLevel, int visitsToNextLoyaltyLevel) GetCurrentLoyaltyLevel(
        IReadOnlyCollection<LoyaltyLevel> levels, int visitsCount)
    {
        var currentLoyaltyLevel = 0;
        var visitsToNextLoyaltyLevel = 0;

        if (levels.Count > 0)
        {
            var nextLevel = levels.FirstOrDefault(l => visitsCount < l.VisitsUpperBound);
            if (nextLevel != null)
            {
                currentLoyaltyLevel = nextLevel.Id;
                visitsToNextLoyaltyLevel = nextLevel.VisitsUpperBound - visitsCount;
            }
            else
            {
                currentLoyaltyLevel = levels.Max(l => l.Id);
                visitsToNextLoyaltyLevel = 0;
            }
        }

        return (currentLoyaltyLevel, visitsToNextLoyaltyLevel);
    }
}