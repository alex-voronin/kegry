using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using Serilog;
using UserProfileModule.Contracts.Events;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.UserProfile;

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

        var user = await userRepository.GetByIdAsync(@event.UserId, ct: ct);
        if (user == null)
        {
            user = new User
            {
                Id = @event.UserId,
                Email = @event.Email,
                FirstName = @event.FirstName,
                LastName = @event.LastName,
                AvatarImageS3Key = @event.AvatarImageS3Key,
                UpdatedAt = @event.UpdatedAt
            };
        }
        else
        {
            user.FirstName = @event.FirstName;
            user.LastName = @event.LastName;
            user.AvatarImageS3Key = @event.AvatarImageS3Key;
            user.UpdatedAt = @event.UpdatedAt;
        }

        await userRepository.SaveAsync(user, ct);
    }
}