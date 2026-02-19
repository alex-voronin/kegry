namespace EngagementModule.API.Dto;

public record MetNpcDto(
    string Id,
    string Name,
    string AvatarUrl
);

public record GetMetNpcsResponse(MetNpcDto[] MetNpcs);
