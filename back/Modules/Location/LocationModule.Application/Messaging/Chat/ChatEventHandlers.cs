using ChatModule.Contracts.Events;
using LocationModule.Domain.Aggregates;
using LocationModule.Domain.Repositories;
using Serilog;
using Wolverine.Attributes;

namespace LocationModule.Application.Messaging.Chat;

[WolverineHandler]
public static class ChatEventHandlers
{
    public static async Task HandleAsync(
        ChatCreatedEvent @event,
        ITrackedUserPairRepository trackedUserPairRepository,
        CancellationToken ct)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(ChatEventHandlers).FullName, @event);

        if (@event.Participants.Length != 2)
        {
            throw new ArgumentException("Only two participants are supported");
        }

        var user1Id = @event.Participants[0].UserId;
        var user2Id = @event.Participants[1].UserId;

        var pair = new TrackedUserPair
        {
            FirstUserId = user1Id,
            SecondUserId = user2Id,
            ExpiresAt = @event.ExpiresAt
        };

        await trackedUserPairRepository.AddAsync(pair, ct);
        await trackedUserPairRepository.SaveChangesAsync(ct);
    }

    public static async Task HandleAsync(
        ChatsDeletedEvent @event,
        ITrackedUserPairRepository trackedUserPairRepository,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(ChatEventHandlers).FullName, @event);

        var userIds = @event.ParticipantIdsFromDeletedChats.SelectMany(p => p).Distinct().ToArray();
        await trackedUserPairRepository.DeleteByUserIds(userIds, ct);
        await trackedUserPairRepository.SaveChangesAsync(ct);
    }
}