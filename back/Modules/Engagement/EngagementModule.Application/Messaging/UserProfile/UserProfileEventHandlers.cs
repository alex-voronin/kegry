using EngagementModule.Domain.Aggregates;
using EngagementModule.Domain.Repositories;
using Serilog;
using UserProfileModule.Contracts.Events;
using Wolverine.Attributes;

namespace EngagementModule.Application.Messaging.UserProfile;

[WolverineHandler]
public static class UserProfileEventHandlers
{
    public static async Task HandleAsync(
        UserProfileChangedEvent @event,
        IUserRepository userRepository,
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
                SeenNpcAt = DateTimeOffset.MinValue
            };
        }
        await userRepository.SaveAsync(user, ct);
    }
}