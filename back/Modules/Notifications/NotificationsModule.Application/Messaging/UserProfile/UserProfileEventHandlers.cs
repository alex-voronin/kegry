using NotificationModule.Domain.Aggregates;
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
        var user = await userRepository.GetByIdAsync(@event.UserId, ct: ct);

        if (user == null)
        {
            user = new User
            {
                Id = @event.UserId,
                Email = @event.Email,
                FirstName = @event.FirstName,
                LastName = @event.LastName,
                AvatarImageUrl = @event.AvatarImageUrl
            };
        }
        else
        {
            user.FirstName = @event.FirstName;
            user.LastName = @event.LastName;
            user.AvatarImageUrl = @event.AvatarImageUrl;
        }

        await userRepository.SaveAsync(user, ct);
    }
}