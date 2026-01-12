using ChatModule.Contracts.Events;
using ChatModule.Domain.Services;
using FeedModule.Contracts.Events;
using Wolverine.Attributes;

namespace ChatModule.Application.Messaging.Feed;

[WolverineHandler]
public class FeedEventHandlers
{
    [Transactional]
    public static async Task<ChatCreatedEvent> HandleAsync(
        MeetingArrangedEvent @event,
        IChatService chatService,
        CancellationToken ct = default)
    {
        await chatService.CreateChatAsync(
            firstUserId: @event.FirstUserId,
            secondUserId: @event.SecondUserId,
            expiresAt: @event.MeetingExpiresAt,
            ct: ct);

        return new ChatCreatedEvent()
        {
            FirstUserId = @event.FirstUserId,
            FirstUserFullName = @event.FirstUserFullName,
            SecondUserId = @event.SecondUserId,
            SecondUserFullName = @event.SecondUserFullName
        };
    }
}