using EngagementModule.Domain.Aggregates;
using EngagementModule.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shared.Infrastructure.Persistence;

namespace EngagementModule.Infrastructure.Persistence.Repositories;

public class EfCoreNpcRepository : Repository<EngagementContext, Npc>, INpcRepository
{
    private const string NpcCountCacheKey = "npc_count";
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;

    public EfCoreNpcRepository(
        EngagementContext context,
        IMemoryCache cache,
        TimeProvider timeProvider) : base(context)
    {
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public async Task<Npc> GetAnyAsync(bool isSecret, CancellationToken ct = default)
    {
        var npcCount = await GetNpcCountAsync(isSecret);
        if (npcCount == 0)
        {
            throw new InvalidOperationException("Npc not found");
        }

        var randomIndex = Random.Shared.Next(npcCount);
        return await Context.Npcs
            .Where(n => n.IsSecret == isSecret)
            .Skip(randomIndex)
            .FirstAsync(ct);
    }

    private async Task<int> GetNpcCountAsync(bool isSecret)
    {
        return await _cache.GetOrCreateAsync(NpcCountCacheKey, async entry =>
        {
            var count = await Context.Npcs
                .Where(n => n.IsSecret == isSecret)
                .CountAsync();
            var now = _timeProvider.GetUtcNow();
            entry.AbsoluteExpiration = count > 0 ? now + TimeSpan.FromDays(1) : now;
            return count;
        });
    }
}