using ChatModule.Contracts.Events;
using PostHog;
using Wolverine.Attributes;

namespace AnalyticsModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatAnalyticsHandlers(IPostHogClient postHogClient)
{
    public void Handle(ChatMessageSentEvent @event)
    {
        postHogClient.Capture(
            distinctId: @event.FromUserId.ToString(),
            eventName: AnalyticsEvents.Chat.MessageSent);
    }
}
