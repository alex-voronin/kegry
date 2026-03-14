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
    Task<LoyaltyLevelWithDiscount> GetLoyaltyLevelWithDiscountAsync(
        Guid userId,
        Guid venueId,
        CancellationToken ct = default);

    Task CheckInToVenue(
        Guid userId, Guid venueId,
        double userLatitude, double userLongitude,
        CancellationToken ct = default);
}

public class LoyaltyService : ILoyaltyService, IScoped
{
    internal const double MaxCheckInDistanceMeters = 100.0;
    internal const double CheckInCooldownHours = 12;

    private readonly IVenueRepository _venueRepository;
    private readonly IVisitRepository _visitRepository;
    private readonly ILoyaltyLevelRepository _loyaltyLevelRepository;
    private readonly IDiscountRepository _discountRepository;

    public LoyaltyService(
        IVenueRepository venueRepository,
        IVisitRepository visitRepository,
        ILoyaltyLevelRepository loyaltyLevelRepository,
        IDiscountRepository discountRepository)
    {
        _venueRepository = venueRepository;
        _visitRepository = visitRepository;
        _loyaltyLevelRepository = loyaltyLevelRepository;
        _discountRepository = discountRepository;
    }

    public async Task CheckInToVenue(
        Guid userId, Guid venueId,
        double userLatitude, double userLongitude,
        CancellationToken ct = default)
    {
        var venue = await _venueRepository.GetByIdAsync(
            venueId,
            asNoTracking: true,
            ct: ct);

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

        var lastVisit = await _visitRepository.GetLastVisitAsync(
            userId: userId,
            venueId: venue.Id,
            ct: ct);

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
        await _visitRepository.SaveChangesAsync(ct);
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

        var visitsCount = await _visitRepository.GetVisitCountAsync(userId, venueId, ct);

        var (currentLoyaltyLevel, visitsToNextLoyaltyLevel) = GetCurrentLoyaltyLevel(
            venue.LoyaltyLevels, visitsCount);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(venue.TimeZoneId);
        var venueLocalTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
        var today = venueLocalTime.DayOfWeek;

        var applicableDiscounts = venue.Discounts
            .Where(d => d.Days.Any(day => day.DayOfWeek == today))
            // If LoyaltyLevelId is null, it's for everyone. Otherwise, requires the user to be at least that level.
            .Where(d => d.LoyaltyLevelId == null || currentLoyaltyLevel >= d.LoyaltyLevelId)
            .ToList();

        var maxDiscount = applicableDiscounts.Count > 0 ? applicableDiscounts.Max(d => d.Percent) : 0;
        return new LoyaltyLevelWithDiscount(currentLoyaltyLevel, visitsCount, visitsToNextLoyaltyLevel, maxDiscount);
    }

    private static (int currentLoyaltyLevel, int visitsToNextLoyaltyLevel) GetCurrentLoyaltyLevel(
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