namespace LocationModule.Domain.Aggregates;

public static class LocationHaversineGeoExtensions
{
    private const double R = 6371000;

    public static double DistanceMetersTo(this Location first, double latitude, double longitude)
    {
        return DistanceMeters(first.Latitude, first.Longitude, latitude, longitude);
    }

    private static double DistanceMeters(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * R * Math.Asin(Math.Sqrt(a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}