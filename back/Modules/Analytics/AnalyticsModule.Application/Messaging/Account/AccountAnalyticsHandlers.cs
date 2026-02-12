using SecurityModule.Contracts.Events;
using PostHog;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Account;

[WolverineHandler]
public class AccountAnalyticsHandlers(IPostHogClient postHogClient)
{
    public void Handle(AccountCreatedEvent @event)
    {
        postHogClient.Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.Account.Created,
            properties: new Dictionary<string, object>
            {
                { "email", @event.Email }
            });
    }

    public void Handle(AccountDeletedEvent @event)
    {
        postHogClient.Capture(
            distinctId: @event.Id.ToString(),
            eventName: AnalyticsEvents.Account.Deleted);
    }

    public void Handle(PasswordResetRequestedEvent @event)
    {
        postHogClient.Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.Account.PasswordResetRequested,
            properties: new Dictionary<string, object>
            {
                { "email", @event.Email }
            });
    }
}
