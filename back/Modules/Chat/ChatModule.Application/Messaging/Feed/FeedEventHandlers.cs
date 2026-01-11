using ChatModule.Domain.Services;
using FeedModule.Contracts.Events;
using Wolverine.Attributes;

namespace ChatModule.Application.Messaging.Feed;

[WolverineHandler]
public class FeedEventHandlers
{
    public static Task HandleAsync(
        MeetingArrangedEvent @event,
        IChatService chatService,
        CancellationToken ct = default)
    {
        return chatService.CreateChatAsync(
            firstUserId: @event.FirstUserId,
            secondUserId: @event.SecondUserId,
            expiresAt: @event.MeetingExpiresAt,
            ct: ct);
    }
}