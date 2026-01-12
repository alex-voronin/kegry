using System.Collections.Concurrent;
using ChatModule.Contracts.Events;
using NotificationsModule.Application.Services;
using Serilog;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Chat;

[WolverineHandler]
public class ChatEventHandlers
{
    public static async Task HandleAsync(
        ChatCreatedEvent @event,
        IDebouncedEventHandler<MeetingArrangedNotificationEvent> eventHandler,
        CancellationToken ct = default)
    {
        // We can't use Parallel.ForEachAsync here to speed up handling,
        // because sending push notifications code relies on the notification settings repository internally,
        // and, as we know, DbContext does not support parallel operations.
        foreach (var participant in @event.Participants)
        {
            try
            {
                await eventHandler.HandleAsync(
                    new MeetingArrangedNotificationEvent
                    {
                        UserId = participant.UserId,
                        OtherUserFullName = participant.FullName
                    }, ct);
            }
            catch (Exception e)
            {
                Log.Error(e,
                    "An exception occurred while handling a {EventName}. UserId: {UserId}",
                    nameof(ChatCreatedEvent), participant.UserId);
            }
        }
    }

    public static async Task HandleAsync(
        ChatsDeletedEvent @event,
        IPushNotificationStrategy pushNotificationStrategy,
        CancellationToken ct = default)
    {
        // We can't use Parallel.ForEachAsync here to speed up handling,
        // because sending push notifications code relies on the notification settings repository internally,
        // and, as we know, DbContext does not support parallel operations.
        var errors = new ConcurrentBag<Exception>();
        foreach (var participantIds in @event.ParticipantIdsFromDeletedChats)
        {
            if (participantIds.Length != 2)
            {
                throw new ArgumentException($"A chat can have only two participants, but {participantIds.Length} " +
                                            $"were provided: {string.Join(", ", participantIds)}");
            }

            try
            {
                var firstUserId = participantIds[0];
                var secondUserId = participantIds[1];
                await pushNotificationStrategy.SendGoToRateUserScreenSilent(firstUserId, secondUserId,
                    ct);
                await pushNotificationStrategy.SendGoToRateUserScreenSilent(secondUserId, firstUserId,
                    ct);
            }
            catch (Exception e)
            {
                Log.Error(e,
                    "An exception occurred while handling a {EventName}. UserIds: {UserIds}",
                    nameof(ChatsDeletedEvent), string.Join(", ", participantIds));
                errors.Add(e);
            }
        }

        if (!errors.IsEmpty)
        {
            throw new AggregateException(errors);
        }
    }
}