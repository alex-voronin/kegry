using FeedModule.Contracts.Events;
using NotificationModule.Domain.Repositories;
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
        INotificationsSettingsRepository notificationsSettingsRepository,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(FeedEventHandlers).FullName, @event);

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

                var userIdToPushesEnabledDict = await notificationsSettingsRepository
                    .GetSettingsByIds([firstUserId, secondUserId], ct);

                var firstUserSettings = userIdToPushesEnabledDict.GetValueOrDefault(firstUserId);
                if (firstUserSettings != null && firstUserSettings.PushesEnabled)
                {
                    await pushNotificationStrategy.SendOpenRateUserScreen(
                        userId: firstUserId,
                        rateeUserId: secondUserId,
                        userCulture: firstUserSettings.GetCultureInfo(),
                        ct: ct);
                }

                var secondUserSettings = userIdToPushesEnabledDict.GetValueOrDefault(secondUserId);
                if (secondUserSettings != null && secondUserSettings.PushesEnabled)
                {
                    await pushNotificationStrategy.SendOpenRateUserScreen(
                        userId: secondUserId,
                        rateeUserId: firstUserId,
                        userCulture: secondUserSettings.GetCultureInfo(),
                        ct: ct);
                }
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