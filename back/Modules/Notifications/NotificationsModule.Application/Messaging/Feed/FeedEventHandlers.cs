using FeedModule.Contracts.Events;
using NotificationsModule.Application.Services;
using Serilog;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Feed;

[WolverineHandler]
public static class FeedEventHandlers
{
    public static async Task HandleAsync(
        MeetingsEndedEvent @event,
        IPushNotificationStrategy pushNotificationStrategy,
        CancellationToken ct = default)
    {
        // We can't use Parallel.ForEachAsync here to speed up handling,
        // because sending push notifications code relies on the notification settings repository internally,
        // and, as we know, DbContext does not support parallel operations.
        var errors = new List<Exception>();
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
                await pushNotificationStrategy.SendOpenRateUserScreenSilent(firstUserId, secondUserId,
                    ct);
                await pushNotificationStrategy.SendOpenRateUserScreenSilent(secondUserId, firstUserId,
                    ct);
            }
            catch (Exception e)
            {
                Log.Error(e,
                    "An exception occurred while handling a {EventName}. UserIds: {UserIds}",
                    nameof(MeetingsEndedEvent), string.Join(", ", participantIds));
                errors.Add(e);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(errors);
        }
    }
}