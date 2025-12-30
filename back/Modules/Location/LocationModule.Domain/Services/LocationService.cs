using LocationModule.Domain.Aggregates;
using NetTopologySuite.Geometries;
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
    }

    private readonly TimeProvider _timeProvider;
    private static readonly GeometryFactory GeometryFactory = new GeometryFactory(new PrecisionModel(), 4326);

    public LocationService(ILocationRepository repository, TimeProvider timeProvider) : base(repository)
    {
        _timeProvider = timeProvider;
    }

    public async Task UpdateLocation(Guid userId, double newLat, double newLong, CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        var newCoordinates = GeometryFactory.CreatePoint(new Coordinate(newLong, newLat));

        var existingLocation = await Repository.GetByIdAsync(userId, ct: ct);

        if (existingLocation == null)
        {
            var newLocation = new Location
            {
                UserId = userId,
                Coordinates = newCoordinates,
                Timestamp = now
            };
            await Repository.AddAsync(newLocation, ct: ct);
        }
        else
        {
            if (CanUpdate(existingLocation, newLat, newLong, now))
            {
                existingLocation.Update(newCoordinates, now);
            }
        }

        await Repository.SaveChangesAsync(ct: ct);
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