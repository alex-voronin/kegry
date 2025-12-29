namespace LocationModule.Domain.Aggregates;

public class Location
{
    public required Guid UserId { get; init; }
    
    private double _latitude;
    public required double Latitude
    {
        get => _latitude; 
        init => _latitude = value;
    }
    
    private double _longitude;
    public required double Longitude
    {
        get => _longitude; 
        init => _longitude = value;
    }
    
    private DateTime _timestamp;
    public required DateTime Timestamp 
    { 
        get => _timestamp; 
        init => _timestamp = value; 
    }

    internal void Update(double newLat, double newLong, DateTime timestamp)
    {
        _latitude = newLat;
        _longitude = newLong;
        _timestamp = timestamp;
    }
}
