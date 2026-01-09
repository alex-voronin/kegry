using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace LocationModule.Domain.Aggregates;

public class Location
{
    public required Guid UserId { get; init; }

    private Point _coordinates = null!;
    public required Point Coordinates
    {
        get => _coordinates;
        init => _coordinates = value;
    }

    [NotMapped]
    public double Latitude => Coordinates.Y;
    [NotMapped]
    public double Longitude => Coordinates.X;

    private DateTimeOffset _timestamp;
    public required DateTimeOffset Timestamp
    {
        get => _timestamp;
        init => _timestamp = value;
    }

    internal void Update(Point newCoordinates, DateTimeOffset timestamp)
    {
        _coordinates = newCoordinates;
        _timestamp = timestamp;
    }
}
