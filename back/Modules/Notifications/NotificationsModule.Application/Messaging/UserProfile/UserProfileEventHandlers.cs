using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
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
                AvatarImageS3Key = @event.AvatarImageS3Key
            };
        }
        else
        {
            user.FirstName = @event.FirstName;
            user.LastName = @event.LastName;
            user.AvatarImageS3Key = @event.AvatarImageS3Key;
        }

        await userRepository.SaveAsync(user, ct);
    }
}