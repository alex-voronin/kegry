namespace EngagementModule.API.Dto;

public record MetNpcDto(
    string Id,
    bool IsSecret,
    string Name,
    string AvatarUrl,
    string CoverUrl
);

public record GetMetNpcsResponse(MetNpcDto[] MetNpcs);
