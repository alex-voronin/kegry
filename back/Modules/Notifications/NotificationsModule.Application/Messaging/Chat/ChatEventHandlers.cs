using ChatModule.Contracts.Events;
using NotificationModule.Domain.Repositories;
using NotificationsModule.Application.Services;
using Serilog;
using Wolverine.Attributes;

namespace NotificationsModule.Application.Messaging.Chat;

[WolverineHandler]
public static class ChatEventHandlers
{
    public static async Task HandleAsync(
        ChatCreatedEvent @event,
        IDebouncedEventHandler<MeetingArrangedNotificationEvent> eventHandler,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(ChatEventHandlers).FullName, @event);

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
        ChatParticipantsMetEvent @event,
        IPushNotificationStrategy pushNotificationStrategy,
        INotificationsSettingsRepository notificationsSettingsRepository,
        CancellationToken ct = default)
    {
        Log.Information("{HandlerClass} Handling event {@Event}", typeof(ChatEventHandlers).FullName, @event);

        if (@event.ParticipantIds.Length != 2)
        {
            throw new ArgumentException($"A chat can have only two participants, but {@event.ParticipantIds.Length} " +
                                        $"were provided: {string.Join(", ", @event.ParticipantIds.Length)}");
        }

        var userIdToSettingsDict = await notificationsSettingsRepository
            .GetSettingsByIds(@event.ParticipantIds, ct);

        foreach (var participantId in @event.ParticipantIds)
        {
            try
            {
                var settings = userIdToSettingsDict.GetValueOrDefault(participantId);
                if (settings != null && settings.PushesEnabled)
                {
                    await pushNotificationStrategy.SendEndMeetingSuggestion(
                        userId: participantId, 
                        userCulture: settings.GetCultureInfo(),
                        ct: ct);
                }
            }
            catch (Exception e)
            {
                Log.Error(e,
                    "An exception occurred while handling a {EventName}. UserId: {UserId}",
                    nameof(ChatParticipantsMetEvent), participantId);
            }
        }
    }
}