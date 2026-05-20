using Microsoft.AspNetCore.Mvc;
using SharedCore.Api.Dto;

namespace VenueModule.API.B2C.Dto;

public record GetVenuesRequest(
    [FromQuery] double Latitude,
    [FromQuery] double Longitude,
    [FromQuery] int Offset = 0,
    [FromQuery] int Limit = 20,
    VenueFilterDto? Filter = null) : PaginatedRequest(Offset, Limit);
