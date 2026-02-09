using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.Repositories;
using Serilog;
using UserProfileModule.Contracts.Events;
using Wolverine.Attributes;

namespace SecurityModule.Application.Messaging.UserProfile;

[WolverineHandler]
public static class UserProfileEventHandlers
{
    public static async Task HandleAsync(
        UserProfileChangedEvent @event,
        IUserRepository userRepository,
        CancellationToken ct)
    {
        if (!@event.IsNewProfile)
        {
            return;
        }

        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(UserProfileEventHandlers).FullName, @event);

        var user = await userRepository.GetByIdAsync(@event.UserId, ct: ct);
        if (user == null)
        {
            Log.Warning("User with id {UserId} not found", @event.UserId);
            return;
        }

        user.RequiredAction = RequiredAction.None;
        await userRepository.SaveAsync(user, ct);
    }
}
