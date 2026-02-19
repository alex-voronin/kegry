namespace EngagementModule.API.Dto;

public record MetNpcDto(
    string Id,
    string Name,
    string AvatarUrl,
    string CoverUrl
);

public record GetMetNpcsResponse(MetNpcDto[] MetNpcs);
