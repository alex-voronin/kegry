using SharedCore.Geolocation;

namespace LocationModule.Domain.Aggregates;

public static class LocationHaversineGeoExtensions
{
    public static double DistanceMetersTo(this Location first, double latitude, double longitude)
    {
        return Haversine.DistanceMeters(first.Latitude, first.Longitude, latitude, longitude);
    }
}