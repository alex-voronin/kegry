using Microsoft.AspNetCore.Mvc;

namespace SharedCore.Api.Dto;

public record PaginatedRequest([FromQuery] int Offset, [FromQuery] int Limit);