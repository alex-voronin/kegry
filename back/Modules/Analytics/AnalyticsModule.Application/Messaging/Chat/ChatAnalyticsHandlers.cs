using AnalyticsModule.Application.Services;
using ChatModule.Contracts.Events;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Chat;

[WolverineHandler]
public static class ChatAnalyticsHandlers
{
    public static void Handle(ChatMessageSentEvent @event, IAnalyticsService analyticsService)
    {
        analyticsService.Capture(
            distinctId: @event.FromUserId.ToString(),
            eventName: AnalyticsEvents.Chat.MessageSent);
    }
}
