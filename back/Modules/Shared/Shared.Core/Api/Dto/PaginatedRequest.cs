using Microsoft.AspNetCore.Mvc;

namespace SharedCore.Api.Dto;

public record PaginatedRequest([FromQuery] int Offset = 0, [FromQuery] int Limit = 20);