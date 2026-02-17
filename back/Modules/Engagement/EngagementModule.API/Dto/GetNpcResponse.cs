namespace EngagementModule.API.Dto;

public record NpcDto(string Id, string Name, string AvatarUrl);

public record GetNpcResponse(NpcDto? Npc);