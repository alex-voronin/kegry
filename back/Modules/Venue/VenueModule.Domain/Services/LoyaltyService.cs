using System.Security.Cryptography;
using System.Text;
using GeoTimeZone;
using SharedCore.Geolocation;
using SharedCore.Services.LifeTimes;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;
using Microsoft.Extensions.Localization;
using VenueModule.Domain.Resources;

namespace VenueModule.Domain.Services;

public record CurrentAndNextLevelDiscount(int? Discount, int? NextLevelDiscount, bool IsCustomDiscount);

public record LoyaltyLevelWithDiscount(
    int UserLoyaltyLevel,
    int VisitsCount,
    int VisitsLowerBound,
    int? VisitsUpperBound,
    int? Discount,
    int? NextLevelDiscount);

public interface ILoyaltyService
{
    Task<(List<DiscountWithDistance> Discounts, int TotalCount)> GetAvailableDiscountsAsync(
        Guid userId,
        double latitude,
        double longitude,
        int offset,
        int limit,
        CancellationToken ct);

    public Task<LoyaltyLevelWithDiscount> GetLoyaltyLevelWithDiscountAsync(
        Guid userId,
        Venue venue,
        CancellationToken ct = default);

    public Task<Dictionary<DayOfWeek, CurrentAndNextLevelDiscount>> GetDiscountByWeekdayAsync(
        Guid userId,
        Guid venueId,
        CancellationToken ct = default);

    Task<(Guid, string? error)> CheckInToVenue(
        Guid userId, Guid qrCode,
        double? userLatitude, double? userLongitude,
        string? pinCode,
        CancellationToken ct = default);

    string GenerateDailyTotpPinCode(Venue venue);
}

internal class LoyaltyService : ILoyaltyService, IScoped
{
    internal const double MaxCheckInDistanceMeters = 150.0;
    internal const double CheckInCooldownHours = 12;

    private readonly IVenueRepository _venueRepository;
    private readonly IVisitRepository _visitRepository;
    private readonly IUserLoyaltyRepository _userLoyaltyRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<VenueLocalization> _localizer;

    public LoyaltyService(
        IVenueRepository venueRepository,
        IVisitRepository visitRepository,
        IUserLoyaltyRepository userLoyaltyRepository,
        IDiscountRepository discountRepository,
        TimeProvider timeProvider,
        IStringLocalizer<VenueLocalization> localizer)
    {
        _venueRepository = venueRepository;
        _visitRepository = visitRepository;
        _userLoyaltyRepository = userLoyaltyRepository;
        _discountRepository = discountRepository;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    public async Task<(List<DiscountWithDistance> Discounts, int TotalCount)> GetAvailableDiscountsAsync(
        Guid userId,
        double latitude,
        double longitude,
        int offset,
        int limit,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow();
        var timezoneId = TimeZoneLookup.GetTimeZone(latitude, longitude).Result;
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(now.DateTime, timeZone);
        var dayOfWeek = userLocalTime.DayOfWeek;
        return await _discountRepository.GetAvailableDiscountsInRadiusAsync(
            userId: userId,
            latitude: latitude,
            longitude: longitude,
            dayOfWeek: dayOfWeek,
            offset: offset,
            limit: limit,
            ct: ct);
    }

    public async Task<(Guid, string? error)> CheckInToVenue(
        Guid userId, Guid qrCode,
        double? userLatitude, double? userLongitude,
        string? pinCode,
        CancellationToken ct = default)
    {
        var transaction = await _venueRepository
            .And(_userLoyaltyRepository)
            .StartTransactionAsync(ct);

        var (venue, lastVisit, userLoyalty) = await _venueRepository.GetVenueWithLastVisitsAndUserLoyaltyAsync(
            qrCode: qrCode,
            userId: userId,
            ct: ct);

        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(qrCode);
        }

        if (userLoyalty == null)
        {
            userLoyalty = new UserLoyalty
            {
                UserId = userId,
                CompanyId = venue.CompanyId,
                LoyaltyLevelId = 0,
                VisitsCount = 0
            };
            await _userLoyaltyRepository.AddAsync(userLoyalty, ct);
        }

        if (pinCode == null && (userLatitude == null || userLongitude == null))
        {
            throw new ArgumentException("Either PIN or latitude and longitude must be provided");
        }

        string? error;
        if (pinCode != null)
        {
            if (!venue.IsPinCheckInAllowed)
            {
                return (venue.Id, "PIN check-in is disabled");
            }

            error = CheckInByPin(pinCode, venue);
        }
        else
        {
            error = CanCheckInUsingLocation(userLatitude!.Value, userLongitude!.Value, venue);
        }

        if (error != null)
        {
            return (venue.Id, error);
        }

        var now = _timeProvider.GetUtcNow();
        if (lastVisit != null && lastVisit.CheckedInAt > now - TimeSpan.FromHours(CheckInCooldownHours))
        {
            return (venue.Id, _localizer.GetString("AlreadyCheckedInError"));
        }

        var visit = new Visit
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            VenueId = venue.Id,
            CheckedInAt = now
        };
        await _visitRepository.AddAsync(visit, ct);

        var newVisitsCount = userLoyalty.VisitsCount + 1;
        var (currentLoyaltyLevelId, _, _) = GetCurrentLoyaltyLevel(venue.Company.LoyaltyLevels, newVisitsCount);

        userLoyalty.LoyaltyLevelId = currentLoyaltyLevelId;
        userLoyalty.VisitsCount = newVisitsCount;

        await _visitRepository.SaveChangesAsync(ct);
        await _userLoyaltyRepository.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return (venue.Id, null);
    }

    private string? CanCheckInUsingLocation(double userLatitude, double userLongitude, Venue venue)
    {
        var distanceMeters = Haversine.DistanceMeters(
            userLatitude, userLongitude,
            venue.Coordinates.Y, venue.Coordinates.X);

        if (distanceMeters > MaxCheckInDistanceMeters)
        {
            return _localizer.GetString("NotInVenueError");
        }

        return null;
    }

    private string? CheckInByPin(string pinCode, Venue venue)
    {
        var expectedPinCode = venue.PinCode.Length > 0 ? venue.PinCode : GenerateDailyTotpPinCode(venue);
        if (expectedPinCode != pinCode)
        {
            return _localizer.GetString("InvalidPinCodeError");
        }

        return null;
    }

    public async Task<LoyaltyLevelWithDiscount> GetLoyaltyLevelWithDiscountAsync(
        Guid userId,
        Venue venue,
        CancellationToken ct = default)
    {
        var userLoyalty = await _userLoyaltyRepository.GetByUserAndCompanyId(userId, venue.CompanyId, ct);
        var visitsCount = userLoyalty?.VisitsCount ?? 0;

        var (computedLoyaltyLevel, visitsLowerBound, visitsUpperBound) = GetCurrentLoyaltyLevel(
            venue.Company.LoyaltyLevels, visitsCount);
        var currentLoyaltyLevel = userLoyalty?.LoyaltyLevelId ?? computedLoyaltyLevel;

        var now = _timeProvider.GetUtcNow();
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(venue.TimeZoneId);
        var venueLocalTime = TimeZoneInfo.ConvertTimeFromUtc(now.DateTime, timeZone);
        var today = venueLocalTime.DayOfWeek;

        var (currentLevelDiscount, nextLevelDiscount) = ComputeCurrentAndNextLevelDiscount(venue.Discounts,
            today, currentLoyaltyLevel);

        return new LoyaltyLevelWithDiscount(
            UserLoyaltyLevel: currentLoyaltyLevel,
            VisitsCount: visitsCount,
            VisitsLowerBound: visitsLowerBound,
            VisitsUpperBound: visitsUpperBound,
            Discount: currentLevelDiscount,
            NextLevelDiscount: nextLevelDiscount);
    }

    public async Task<Dictionary<DayOfWeek, CurrentAndNextLevelDiscount>> GetDiscountByWeekdayAsync(
        Guid userId,
        Guid venueId,
        CancellationToken ct = default)
    {
        var venue = await _venueRepository.GetByIdWithAllRelationsAsync(venueId, ct);
        if (venue == null)
        {
            throw VenueExceptions.VenueNotFound(venueId);
        }

        var userLoyalty = await _userLoyaltyRepository.GetByUserAndCompanyId(userId, venue.CompanyId, ct);
        var visitsCount = userLoyalty?.VisitsCount ?? 0;

        var (computedLoyaltyLevel, _, _) = GetCurrentLoyaltyLevel(
            venue.Company.LoyaltyLevels, visitsCount);
        var currentLoyaltyLevel = userLoyalty?.LoyaltyLevelId ?? computedLoyaltyLevel;

        var result = new Dictionary<DayOfWeek, CurrentAndNextLevelDiscount>();
        foreach (var dayOfWeek in Enum.GetValues<DayOfWeek>())
        {
            var (currentLevelDiscount, nextLevelDiscount) = ComputeCurrentAndNextLevelDiscount(venue.Discounts,
                dayOfWeek, currentLoyaltyLevel);

            result[dayOfWeek] = new CurrentAndNextLevelDiscount(
                Discount: currentLevelDiscount,
                NextLevelDiscount: nextLevelDiscount,
                IsCustomDiscount: currentLevelDiscount == null);
        }

        return result;
    }

    private const string TotpSalt = "salt-848Clfjevnz52i4u5y";

    public string GenerateDailyTotpPinCode(Venue venue)
    {
        var now = _timeProvider.GetUtcNow();
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(venue.TimeZoneId);
        var venueLocalTime = TimeZoneInfo.ConvertTimeFromUtc(now.DateTime, timeZone);
        var dateStr = venueLocalTime.ToString("yyyy-MM-dd");
        var input = dateStr + venue.Id + TotpSalt;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        // Take 4 bytes and convert to positive number
        var number = Math.Abs(BitConverter.ToInt32(bytes, 0));
        var pin = number % 10000;

        // Return string with 4 leading zeroes
        return pin.ToString("D4");
    }

    private static (int? currentLevelDiscount, int? nextLevelDiscount) ComputeCurrentAndNextLevelDiscount(
        IReadOnlyCollection<Discount> discounts, DayOfWeek dayOfWeek, int currentLoyaltyLevel)
    {
        var hasCustomDiscounts = discounts.Any(d => d.Days.Any(day => day.DayOfWeek == dayOfWeek) && d.IsCustom);
        if (hasCustomDiscounts)
        {
            return (null, null);
        }

        var applicableDiscounts = discounts
            .Where(d => d.Percent != null)
            .Where(d => d.Days.Any(day => day.DayOfWeek == dayOfWeek))
            .Where(d => currentLoyaltyLevel >= d.LoyaltyLevelId)
            .Where(d => d.LoyaltyLevel.IsActive)
            .ToList();

        var nextLevelDiscount = discounts
            .Where(d => d.Percent != null)
            .Where(d => d.Days.Any(day => day.DayOfWeek == dayOfWeek))
            .Where(d => d.LoyaltyLevelId == currentLoyaltyLevel + 1)
            .Where(d => d.LoyaltyLevel.IsActive)
            .FirstOrDefault();
        var currentDiscountPercent = applicableDiscounts.Count > 0 ? applicableDiscounts.Max(d => d.Percent) : 0;
        return (currentDiscountPercent ?? 0, nextLevelDiscount?.Percent);
    }

    internal static (int currentLoyaltyLevel, int visitsLowerBound, int? visitsUpperBound) GetCurrentLoyaltyLevel(
        IReadOnlyCollection<LoyaltyLevel> levels, int visitsCount)
    {
        var sortedLevels = levels
            .Where(l => l.IsActive)
            .OrderBy(l => l.VisitsUpperBound)
            .ToArray();

        if (sortedLevels.Length == 0)
        {
            return (0, 0, null);
        }

        var maxLevel = sortedLevels[^1];

        var currentLoyaltyLevel = maxLevel.Id;
        var visitsLowerBound = maxLevel.VisitsUpperBound;
        int? visitsUpperBound = null;

        LoyaltyLevel? prevLevel = null;
        foreach (var level in sortedLevels)
        {
            if (visitsCount < level.VisitsUpperBound)
            {
                currentLoyaltyLevel = level.Id;
                visitsLowerBound = prevLevel?.VisitsUpperBound ?? 0;
                visitsUpperBound = level.VisitsUpperBound;
                break;
            }

            prevLevel = level;
        }

        if (currentLoyaltyLevel == maxLevel.Id)
        {
            visitsUpperBound = null; // as we can't grow anymore
        }

        return (currentLoyaltyLevel, visitsLowerBound, visitsUpperBound);
    }
}