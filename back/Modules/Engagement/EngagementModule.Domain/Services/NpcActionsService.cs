using EngagementModule.Domain.Aggregates;
using EngagementModule.Domain.Exceptions;
using EngagementModule.Domain.Repositories;
using SharedCore.Services.LifeTimes;

namespace EngagementModule.Domain.Services;

internal class NpcActionsService : INpcActionsService, IScoped
{
    internal const int NpcMeetingTimeoutHours = 24;

    private readonly INpcRepository _npcRepository;
    private readonly IUserRepository _userRepository;
    private readonly TimeProvider _timeProvider;

    public NpcActionsService(INpcRepository npcRepository, IUserRepository userRepository, TimeProvider timeProvider)
    {
        _npcRepository = npcRepository;
        _userRepository = userRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Npc?> GetNpcAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct: ct);
        if (user == null)
        {
            throw EngagementExceptions.UserNotFound(userId);
        }

        if (_timeProvider.GetUtcNow() - user.SeenNpcAt < TimeSpan.FromHours(NpcMeetingTimeoutHours))
        {
            return null;
        }

        user.SeenNpcAt = _timeProvider.GetUtcNow();
        var npc = await _npcRepository.GetAnyAsync(ct);
        await _userRepository.SaveChangesAsync(ct: ct);

        return npc;
    }

    public async Task<Npc> InviteNpcAsync(Guid userId, Guid npcId, CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(userId, includes: [u => u.MetNpcs], ct: ct);
        if (user == null)
        {
            throw EngagementExceptions.UserNotFound(userId);
        }

        var npc = await _npcRepository.GetByIdAsync(npcId, ct: ct);
        if (npc == null)
        {
            throw EngagementExceptions.NpcNotFound(npcId);
        }

        if (user.MetNpcs.All(n => n.Id != npcId))
        {
            user.AddMetNpc(npc);
            await _userRepository.SaveChangesAsync(ct);
        }

        return npc;
    }
}