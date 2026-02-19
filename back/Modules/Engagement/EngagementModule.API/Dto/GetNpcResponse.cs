namespace EngagementModule.API.Dto;

public record NpcDto(string Id, string Name, string AvatarUrl, string CoverUrl);

public record GetNpcResponse(NpcDto? Npc);