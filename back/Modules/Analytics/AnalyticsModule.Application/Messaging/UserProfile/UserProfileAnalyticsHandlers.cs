using UserProfileModule.Contracts.Events;
using PostHog;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.UserProfile;

[WolverineHandler]
public class UserProfileAnalyticsHandlers(IPostHogClient postHogClient)
{
    public async Task Handle(UserProfileChangedEvent @event)
    {
        if (@event.IsNewProfile)
        {
            postHogClient.Capture(
                distinctId: @event.UserId.ToString(),
                eventName: AnalyticsEvents.UserProfile.Created);
        }
        else
        {
            postHogClient.Capture(
                distinctId: @event.UserId.ToString(),
                eventName: AnalyticsEvents.UserProfile.Updated);
        }
        
        await postHogClient.IdentifyAsync(
            distinctId: @event.UserId.ToString(),
            email: @event.Email,
            name: $"{@event.FirstName} {@event.LastName}");
    }
}
