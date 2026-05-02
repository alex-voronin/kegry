namespace VenueModule.API.B2C.Dto;

public record CheckInRequest(
    double? Latitude,
    double? Longitude,
    string? PinCode);
