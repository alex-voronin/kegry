using AnalyticsModule.Application.Services;
using SecurityModule.Contracts.Events;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Account;

[WolverineHandler]
public static class AccountAnalyticsHandlers
{
    public static void Handle(AccountCreatedEvent @event, IAnalyticsService analyticsService)
    {
        analyticsService.Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.Account.Created,
            properties: new Dictionary<string, object>
            {
                { AnalyticsEvents.Keys.Email, @event.Email },
            });
    }

    public static void Handle(EmailConfirmationRequestedEvent @event, IAnalyticsService analyticsService)
    {
        analyticsService.Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.Account.EmailConfirmationRequests,
            properties: new Dictionary<string, object>
            {
                { AnalyticsEvents.Keys.Email, @event.Email },
            });
    }

    public static void Handle(AccountDeletedEvent @event, IAnalyticsService analyticsService)
    {
        analyticsService.Capture(
            distinctId: @event.Id.ToString(),
            eventName: AnalyticsEvents.Account.Deleted);
    }

    public static void Handle(PasswordResetRequestedEvent @event, IAnalyticsService analyticsService)
    {
        analyticsService.Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.Account.PasswordResetRequested,
            properties: new Dictionary<string, object>
            {
                { AnalyticsEvents.Keys.Email, @event.Email }
            });
    }
}
