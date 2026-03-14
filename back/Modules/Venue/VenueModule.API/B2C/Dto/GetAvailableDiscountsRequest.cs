using Microsoft.AspNetCore.Mvc;
using SharedCore.Api.Dto;

namespace VenueModule.API.B2C.Dto;

public record GetAvailableDiscountsRequest(
    [FromQuery] DayOfWeek DayOfWeek,
    [FromQuery] double Latitude,
    [FromQuery] double Longitude,
    [FromQuery] int Offset,
    [FromQuery] int Limit
) : PaginatedRequest(Offset, Limit);