using ChatModule.Contracts.Events;
using NotificationModule.Domain.Repositories;
using NotificationsModule.Application.Services;
using Serilog;
using SharedCore.Services.LifeTimes;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatMessageSentEventHandlerPushes : IScoped
{
    private readonly IPushNotificationStrategy _pushNotificationStrategy;

    public ChatMessageSentEventHandlerPushes(IPushNotificationStrategy pushNotificationStrategy)
    {
        _pushNotificationStrategy = pushNotificationStrategy;
    }

    public async Task HandleAsync(ChatMessageSentEvent @event, CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}",
            typeof(ChatMessageSentEventHandlerPushes).FullName, @event);
        // TODO: we should respect here user's push settings, but we can't as 
        // chat won't work if pushes are disabled.
        // Must be fixed after switching to Signalr
        await _pushNotificationStrategy.SendChatMessageReceived(@event, ct);
    }
}