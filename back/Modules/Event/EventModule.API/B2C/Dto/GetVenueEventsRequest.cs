using Microsoft.AspNetCore.Mvc;

namespace EventModule.API.B2C.Dto;

public record GetVenueEventsRequest(
    [FromRoute] Guid VenueId,
    [FromQuery] int Offset = 0,
    [FromQuery] int Limit = 20);
