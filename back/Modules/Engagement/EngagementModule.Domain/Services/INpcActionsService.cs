using EngagementModule.Domain.Aggregates;

namespace EngagementModule.Domain.Services;

public interface INpcActionsService
{
    Task<Npc> InviteNpcAsync(Guid userId, Guid npcId, CancellationToken ct);
    Task<Npc?> GetNpcAsync(Guid userId, CancellationToken ct);
}