using EngagementModule.Contracts.Events;
using EngagementModule.Domain.Aggregates;
using EngagementModule.Domain.Repositories;
using Serilog;
using UserProfileModule.Contracts.Events;
using Wolverine;
using Wolverine.Attributes;

namespace EngagementModule.Application.Messaging.UserProfile;

[WolverineHandler]
public static class UserProfileEventHandlers
{
    public static async Task HandleAsync(
        UserProfileChangedEvent @event,
        IUserRepository userRepository,
        INpcRepository npcRepository,
        IMessageBus bus,
        CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(UserProfileEventHandlers).FullName, @event);
        if (!@event.IsNewProfile)
        {
            return;
        }

        var user = await userRepository.GetByIdAsync(@event.UserId, ct: ct);
        if (user == null)
        {
            user = new User
            {
                Id = @event.UserId,
                ReferrerId = @event.ReferrerId,
                SeenNpcAt = DateTimeOffset.MinValue
            };
        }

        await userRepository.SaveAsync(user, ct);

        if (@event.ReferrerId == null)
        {
            return;
        }

        await RewardReferrer(@event.ReferrerId.Value, userRepository, npcRepository, bus, ct);
    }

    private static async Task RewardReferrer(
        Guid referrerId,
        IUserRepository userRepository,
        INpcRepository npcRepository,
        IMessageBus bus,
        CancellationToken ct)
    {
        var referrer = await userRepository.GetByIdAsync(referrerId,
            includes: [u => u.MetNpcs],
            ct: ct);
        if (referrer == null)
        {
            Log.Error("Referrer {ReferrerId} doesn't exist", referrerId);
            return;
        }

        var npc = await npcRepository.GetAnyAsync(isSecret: true, ct: ct);
        var isNewNpcForUser = referrer.MetNpcs.All(n => n.Id != npc.Id);
        if (!isNewNpcForUser)
        {
            return;
        }

        referrer.AddMetNpc(npc);
        await userRepository.SaveChangesAsync(ct);

        await bus.PublishAsync(new ReferrerRewardReceivedEvent(referrerId));
    }
}