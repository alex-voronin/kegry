using ChatModule.Contracts.Events;
using FeedModule.Contracts.Events;
using FeedModule.Domain.Repositories;
using Serilog;
using UserProfileModule.Contracts.Events;
using Wolverine.Attributes;

namespace FeedModule.Application.Messaging.UserProfile;

[WolverineHandler]
public class UserProfileEventHandlers
{
    public static async Task HandleAsync(
        UserProfileChangedEvent @event,
        IFeedUserRelationRepository userRelationRepository,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(UserProfileEventHandlers).FullName, @event);
        if (!@event.IsNewProfile || @event.ReferrerId == null)
        {
            return;
        }

        var userToRefererRelation = await userRelationRepository.GetOrCreateAsync(
            @event.UserId, @event.ReferrerId.Value, ct: ct);
        userToRefererRelation.IsFriend = true;

        var refererToUserRelation = await userRelationRepository.GetOrCreateAsync(
            @event.ReferrerId.Value, @event.UserId, ct: ct);
        refererToUserRelation.IsFriend = true;

        await userRelationRepository.SaveChangesAsync(ct);
    }
}