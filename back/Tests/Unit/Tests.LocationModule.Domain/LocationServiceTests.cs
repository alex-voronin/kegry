using LocationModule.Domain.Aggregates;
using LocationModule.Domain.Services;
using Shouldly;

namespace Tests.LocationModule.Domain;

public class LocationServiceTests
{
    private readonly Location _currentLocation = new Location
    {
        UserId = Guid.NewGuid(),
        Latitude = 50.0,
        Longitude = 30.0,
        Timestamp = DateTime.UtcNow
    };

    [Fact]
    public void CanUpdate_GivenSameLocation_WhenTtlIsElapsed_ThenReturnTrue()
    {
        var now = _currentLocation.Timestamp
            .Add(LocationService.ValidationParameters.CurrentLocationTtl)
            .AddSeconds(1);
        var newLat = _currentLocation.Latitude;
        var newLong = _currentLocation.Longitude;
        
        var result = LocationService.CanUpdate(_currentLocation, newLat, newLong, now);
        result.ShouldBeTrue();
    }
    
    [Fact]
    public void CanUpdate_GivenSameLocation_WhenTtlIsNotElapsed_ThenReturnFalse()
    {
        var now = _currentLocation.Timestamp
            .Add(LocationService.ValidationParameters.CurrentLocationTtl)
            .AddSeconds(-1);
        var newLat = _currentLocation.Latitude;
        var newLong = _currentLocation.Longitude;
        
        var result = LocationService.CanUpdate(_currentLocation, newLat, newLong, now);
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanUpdate_GivenNewLocation_WhenDistanceLessThanThreshold_ThenReturnFalse()
    {
        var now = _currentLocation.Timestamp.AddMinutes(1);
        var newLat = _currentLocation.Latitude + 
                     MetersToDegrees(LocationService.ValidationParameters.ThresholdMeters - 1); 
        var newLong = _currentLocation.Longitude;

        var result = LocationService.CanUpdate(_currentLocation, newLat, newLong, now);
        result.ShouldBeFalse();
    }

    [Fact]
    public void CanUpdate_GivenNewLocation_WhenSpeedTooHigh_ThenReturnFalse()
    {
        var now = _currentLocation.Timestamp.AddSeconds(1);
        var newLat = _currentLocation.Latitude + 
                     MetersToDegrees(LocationService.ValidationParameters.MaxSpeedMps + 1); 
        var newLong = _currentLocation.Longitude;

        var result = LocationService.CanUpdate(_currentLocation, newLat, newLong, now);
        result.ShouldBeFalse();
    }
    
    [Fact]
    public void CanUpdate_GivenNewLocation_WhenValidMovement_ThenReturnTrue()
    {
        var dt = 10;
        var now = _currentLocation.Timestamp.AddSeconds(dt);
        var newLat = _currentLocation.Latitude + 
                     MetersToDegrees((LocationService.ValidationParameters.MaxSpeedMps - 1) * dt); 
        var newLong = _currentLocation.Longitude;

        var result = LocationService.CanUpdate(_currentLocation, newLat, newLong, now);
        result.ShouldBeTrue();
    }

    private double MetersToDegrees(double meters)
    {
        return meters / 111111;
    }
}
