using ChatModule.Contracts.Events;
using ChatModule.Domain.Services;
using FeedModule.Contracts.Events;
using Serilog;
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
        Log.Information("{HandlerClass}, Handling event {@Event}",
            typeof(FeedEventHandlers).FullName, @event);

        await chatService.CreateChatAsync(
            userIds: @event.Participants.Select(p => p.UserId).ToArray(),
            expiresAt: @event.ExpiresAt,
            ct: ct);

        return new ChatCreatedEvent()
        {
            ExpiresAt = @event.ExpiresAt,
            Participants = @event.Participants
                .Select(p => new ChatCreatedEvent.Participant(p.UserId, p.FullName))
                .ToArray()
        };
    }
}