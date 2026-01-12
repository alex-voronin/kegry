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
            userIds: @event.Participants.Select(p => p.UserId).ToArray(),
            expiresAt: @event.MeetingExpiresAt,
            ct: ct);

        return new ChatCreatedEvent()
        {
            Participants = @event.Participants
                .Select(p => new ChatCreatedEvent.Participant(p.UserId, p.FullName))
                .ToArray()
        };
    }
}