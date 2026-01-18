using ChatModule.Contracts.Events;
using FeedModule.Contracts.Events;
using FeedModule.Domain.Repositories;
using Serilog;
using Wolverine.Attributes;

namespace FeedModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatEventHandlers
{
    public static async Task HandleAsync(
        ChatsDeletedEvent @event,
        IFeedUserRelationRepository userRelationRepository,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(ChatEventHandlers).FullName, @event);

        var userIds = @event.ParticipantIdsFromDeletedChats.SelectMany(ids => ids).Distinct().ToArray();
        await userRelationRepository.SetShouldRateAsync(userIds, ct);
        await userRelationRepository.ResetMeetingsAsync(userIds, ct);
        var meetingEndedEvent = new MeetingsEndedEvent()
        {
            ParticipantIdsFromDeletedChats = @event.ParticipantIdsFromDeletedChats,
        };
        await userRelationRepository.PublishEventAsync(meetingEndedEvent);
        await userRelationRepository.SaveChangesAndFlushMessagesAsync(ct);
    }
}