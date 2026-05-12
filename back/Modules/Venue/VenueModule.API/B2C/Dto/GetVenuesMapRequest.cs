namespace VenueModule.API.B2C.Dto;

using Microsoft.AspNetCore.Mvc;

public record GetVenuesMapRequest(
    [FromQuery] double MinLat,
    [FromQuery] double MaxLat,
    [FromQuery] double MinLng,
    [FromQuery] double MaxLng);
