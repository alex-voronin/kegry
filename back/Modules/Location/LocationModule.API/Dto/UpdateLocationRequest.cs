namespace LocationModule.API.Dto;

public class UpdateLocationRequest
{
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}