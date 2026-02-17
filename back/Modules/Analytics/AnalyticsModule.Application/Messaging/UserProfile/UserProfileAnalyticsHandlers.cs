using AnalyticsModule.Application.Services;
using UserProfileModule.Contracts.Events;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.UserProfile;

[WolverineHandler]
public static class UserProfileAnalyticsHandlers
{
    public static async Task Handle(UserProfileChangedEvent @event, IAnalyticsService analyticsService)
    {
        if (@event.IsNewProfile)
        {
            analyticsService.Capture(
                distinctId: @event.UserId.ToString(),
                eventName: AnalyticsEvents.UserProfile.Created);
        }
        else
        {
            analyticsService.Capture(
                distinctId: @event.UserId.ToString(),
                eventName: AnalyticsEvents.UserProfile.Updated);
        }

        await analyticsService.IdentifyAsync(
            distinctId: @event.UserId.ToString(),
            email: @event.Email,
            name: $"{@event.FirstName} {@event.LastName}");
    }
}
