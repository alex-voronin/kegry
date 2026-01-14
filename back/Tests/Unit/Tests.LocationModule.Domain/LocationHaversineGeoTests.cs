using LocationModule.Domain.Aggregates;
using NetTopologySuite.Geometries;
using Shouldly;
using Location = LocationModule.Domain.Aggregates.Location;

namespace Tests.LocationModule.Domain;

public class LocationHaversineGeoTests
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    [Fact]
    public void DistanceMetersTo_WhenSamePoints_ThenReturnsZero()
    {
        var location = new Location
        {
            UserId = Guid.NewGuid(),
            Coordinates = GeometryFactory.CreatePoint(new Coordinate(30.0, 50.0)),
            Timestamp = DateTimeOffset.UtcNow
        };

        var distance = location.DistanceMetersTo(location.Latitude, location.Longitude);
        distance.ShouldBe(0, 0.001);
    }

    [Fact]
    public void DistanceMetersTo_WhenBerlinToParis_ThenReturnsCorrectDistance()
    {
        // Berlin: 52.5200, 13.4050
        // Paris: 48.8566, 2.3522
        // Distance is ~878 km
        var berlin = new Location
        {
            UserId = Guid.NewGuid(),
            Coordinates = GeometryFactory.CreatePoint(new Coordinate(13.4050, 52.5200)),
            Timestamp = DateTimeOffset.UtcNow
        };

        var distance = berlin.DistanceMetersTo(48.8566, 2.3522);
        distance.ShouldBeInRange(877000, 879000);
    }

    [Fact]
    public void DistanceMetersTo_WhenSmallMovement_ThenReturnsCorrectDistance()
    {
        // ~111 meters per 0.001 deg lat
        var start = new Location
        {
            UserId = Guid.NewGuid(),
            Coordinates = GeometryFactory.CreatePoint(new Coordinate(30.0, 50.0)),
            Timestamp = DateTimeOffset.UtcNow
        };

        var distance = start.DistanceMetersTo(50.001, 30.0);
        distance.ShouldBeInRange(110, 112);
    }
}
