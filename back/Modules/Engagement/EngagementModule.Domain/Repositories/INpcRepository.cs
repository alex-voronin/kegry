using EngagementModule.Domain.Aggregates;
using SharedCore.Interfaces;

namespace EngagementModule.Domain.Repositories;

public interface INpcRepository : IRepository<Npc>
{
    Task<Npc> GetAnyAsync(bool isSecret, CancellationToken ct = default);
}