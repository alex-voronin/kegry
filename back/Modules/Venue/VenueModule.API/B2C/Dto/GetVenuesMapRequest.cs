using Microsoft.AspNetCore.Mvc;

namespace VenueModule.API.B2C.Dto;

public record GetVenuesMapRequest(
    [FromQuery] double MinLat,
    [FromQuery] double MaxLat,
    [FromQuery] double MinLng,
    [FromQuery] double MaxLng,
    VenueFilterDto? Filter = null);
