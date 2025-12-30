using LocationModule.Domain.Aggregates;
using Shouldly;

namespace Tests.LocationModule.Domain;

public class LocationHaversineGeoTests
{
    [Fact]
    public void DistanceMetersTo_GivenSamePoints_ThenReturnZero()
    {
        var location = new Location 
        { 
            UserId = Guid.NewGuid(),
            Latitude = 50.0, 
            Longitude = 30.0,
            Timestamp = DateTime.UtcNow
        };
        
        var distance = location.DistanceMetersTo(location.Latitude, location.Longitude);
        distance.ShouldBe(0, 0.001);
    }

    [Fact]
    public void DistanceMetersTo_GivenBerlinToParis_ThenReturnCorrectDistance()
    {
        // Berlin: 52.5200, 13.4050
        // Paris: 48.8566, 2.3522
        // Distance is ~878 km
        var berlin = new Location 
        { 
            UserId = Guid.NewGuid(),
            Latitude = 52.5200, 
            Longitude = 13.4050,
            Timestamp = DateTime.UtcNow
        };
        
        var distance = berlin.DistanceMetersTo(48.8566, 2.3522);
        distance.ShouldBeInRange(877000, 879000);
    }

    [Fact]
    public void DistanceMetersTo_GivenSmallMovement_ThenReturnCorrectDistance()
    {
        // ~111 meters per 0.001 deg lat
        var start = new Location 
        { 
            UserId = Guid.NewGuid(),
            Latitude = 50.0, 
            Longitude = 30.0,
            Timestamp = DateTime.UtcNow
        };
        
        var distance = start.DistanceMetersTo(50.001, 30.0);
        distance.ShouldBeInRange(110, 112);
    }
}
