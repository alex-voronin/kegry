using SharedCore.Geolocation;
using SharedCore.Services.LifeTimes;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.Exceptions;
using VenueModule.Domain.Repositories;

namespace VenueModule.Domain.Services;

public interface ILoyaltyService
{
    Task<int> GetLoyaltyLevelAsync(Guid userId, Guid venueId, CancellationToken ct = default);

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

    public LoyaltyService(IVenueRepository venueRepository, IVisitRepository visitRepository)
    {
        _venueRepository = venueRepository;
        _visitRepository = visitRepository;
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

    public async Task<int> GetLoyaltyLevelAsync(Guid userId, Guid venueId, CancellationToken ct = default)
    {
        var visits = await _visitRepository.GetVisitCountAsync(userId, venueId, ct);
        return visits > 999 ? 999 : visits;
    }
}