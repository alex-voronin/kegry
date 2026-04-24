using LocationModule.Contracts.Events;
using LocationModule.Domain.Aggregates;
using LocationModule.Domain.Repositories;
using NetTopologySuite.Geometries;
using Serilog;
using SharedCore.Services;
using SharedCore.Services.LifeTimes;
using Location = LocationModule.Domain.Aggregates.Location;

namespace LocationModule.Domain.Services;

internal class LocationService : DomainService<ILocationRepository>, ILocationService, IScoped
{
    internal static class ValidationParameters
    {
        internal static readonly TimeSpan CurrentLocationTtl = TimeSpan.FromMinutes(45);
        internal const double ThresholdMeters = 750;
        internal const double MaxSpeedMps = 100; // 360 km/h
        internal const double MetThresholdMeters = 150;
    }

    private readonly ITrackedUserPairRepository _trackedUserPairRepository;
    private readonly TimeProvider _timeProvider;
    private static readonly GeometryFactory GeometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

    public LocationService(
        ILocationRepository repository,
        ITrackedUserPairRepository trackedUserPairRepository,
        TimeProvider timeProvider) : base(repository)
    {
        _trackedUserPairRepository = trackedUserPairRepository;
        _timeProvider = timeProvider;
    }

    public async Task UpdateLocation(Guid userId, double newLat, double newLong, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var newCoordinates = GeometryFactory.CreatePoint(new Coordinate(newLong, newLat));

        var location = await Repository.GetByIdAsync(userId, ct: ct);
        if (location == null)
        {
            location = new Location
            {
                UserId = userId,
                Coordinates = newCoordinates,
                Timestamp = now
            };
            await Repository.AddAsync(location, ct: ct);
        }
        else
        {
            if (CanUpdate(location, newLat, newLong, now))
            {
                location.Update(newCoordinates, now);
            }
        }

        await Repository.SaveChangesAsync(ct);

        await CheckProximityAsync(userId, location, ct);
    }

    private async Task CheckProximityAsync(Guid userId, Location currentLocation, CancellationToken ct)
    {
        var trackedPairs = await _trackedUserPairRepository.GetByUserIdAsync(userId, ct);
        if (trackedPairs.Length == 0) return;

        var pairsRemoved = false;
        foreach (var pair in trackedPairs)
        {
            var otherUserId = pair.FirstUserId == userId ? pair.SecondUserId : pair.FirstUserId;
            var otherLocation = await Repository.GetByIdAsync(otherUserId, ct: ct);

            if (otherLocation == null)
            {
                continue;
            }

            var dist = currentLocation.DistanceMetersTo(otherLocation.Latitude, otherLocation.Longitude);
            if (dist > ValidationParameters.MetThresholdMeters)
            {
                continue;
            }

            Log.Information("Users {FirstUserId} and {SecondUserId} have met.", pair.FirstUserId, pair.SecondUserId);

            _trackedUserPairRepository.Delete(pair);
            pairsRemoved = true;

            var @event = new TrackedUsersMetEvent()
            {
                UserIds = [pair.FirstUserId, pair.SecondUserId]
            };
            await _trackedUserPairRepository.PublishEventAsync(@event);
        }

        if (pairsRemoved)
        {
            await _trackedUserPairRepository.SaveChangesAndFlushMessagesAsync(ct);
        }
    }

    internal static bool CanUpdate(Location existing, double newLat, double newLong, DateTimeOffset now)
    {
        var dt = now - existing.Timestamp;

        if (dt > ValidationParameters.CurrentLocationTtl)
        {
            return true;
        }

        var dist = existing.DistanceMetersTo(newLat, newLong);

        if (dist < ValidationParameters.ThresholdMeters)
        {
            return false;
        }

        var speed = dist / dt.TotalSeconds;
        if (speed > ValidationParameters.MaxSpeedMps)
        {
            return false;
        }

        return true;
    }
}