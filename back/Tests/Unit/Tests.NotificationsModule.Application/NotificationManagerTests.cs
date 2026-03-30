using AutoFixture.Xunit2;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
using NotificationModule.Domain.ValueObjects;
using NotificationsModule.Application.Services;
using NSubstitute;
using Tests.Shared.AutoFixture;

namespace Tests.NotificationsModule.Application;

public class NotificationManagerTests
{
    [Theory, AutoNSubstituteData]
    internal async Task SendMeetingArranged_WhenSettingsNotFound_ThenUsesDefaults(
        [Frozen] INotificationsSettingsRepository settingsRepository,
        [Frozen] IEmailNotificationStrategy emailStrategy,
        [Frozen] IPushNotificationStrategy pushStrategy,
        NotificationManager manager,
        Guid userId,
        string otherUserName)
    {
        settingsRepository.GetByIdOrDefaultAsync(userId,
                asNoTracking: Arg.Any<bool>(), ct: Arg.Any<CancellationToken>())
            .Returns(new NotificationsSettings()
            {
                UserId = userId,
                Locale = SupportedLocales.English,
                PushesEnabled = true,
                EmailsEnabled = false
            }
            );

        await manager.SendMeetingArranged(userId, otherUserName);

        await pushStrategy.Received(1).SendMeetingArranged(userId, otherUserName, Arg.Any<CancellationToken>());
        await emailStrategy.DidNotReceive()
            .SendMeetingArranged(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task SendMeetingArranged_WhenSettingsDisabled_ThenDoesNotSend(
        [Frozen] INotificationsSettingsRepository settingsRepository,
        [Frozen] IEmailNotificationStrategy emailStrategy,
        [Frozen] IPushNotificationStrategy pushStrategy,
        NotificationManager manager,
        Guid userId,
        string otherUserName)
    {
        settingsRepository.GetByIdOrDefaultAsync(userId,
                asNoTracking: Arg.Any<bool>(), ct: Arg.Any<CancellationToken>())
            .Returns(new NotificationsSettings()
            {
                UserId = userId,
                Locale = SupportedLocales.English,
                PushesEnabled = false,
                EmailsEnabled = false
            }
            );

        await manager.SendMeetingArranged(userId, otherUserName);

        await pushStrategy.DidNotReceive()
            .SendMeetingArranged(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await emailStrategy.DidNotReceive()
            .SendMeetingArranged(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task SendMeetingArranged_WhenBothEnabled_ThenSendsBoth(
        [Frozen] INotificationsSettingsRepository settingsRepository,
        [Frozen] IEmailNotificationStrategy emailStrategy,
        [Frozen] IPushNotificationStrategy pushStrategy,
        NotificationManager manager,
        Guid userId,
        string otherUserName)
    {
        settingsRepository.GetByIdOrDefaultAsync(userId,
                asNoTracking: Arg.Any<bool>(), ct: Arg.Any<CancellationToken>())
            .Returns(new NotificationsSettings()
            {
                UserId = userId,
                Locale = SupportedLocales.English,
                PushesEnabled = true,
                EmailsEnabled = true
            }
            );

        await manager.SendMeetingArranged(userId, otherUserName);

        await pushStrategy.Received(1).SendMeetingArranged(userId, otherUserName, Arg.Any<CancellationToken>());
        await emailStrategy.Received(1).SendMeetingArranged(userId, otherUserName, Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    internal async Task SendUserInvited_WhenEmailsEnabledOnly_ThenSendsEmailsOnly(
        [Frozen] INotificationsSettingsRepository settingsRepository,
        [Frozen] IEmailNotificationStrategy emailStrategy,
        [Frozen] IPushNotificationStrategy pushStrategy,
        NotificationManager manager,
        Guid userId)
    {
        settingsRepository.GetByIdOrDefaultAsync(userId,
                asNoTracking: Arg.Any<bool>(), ct: Arg.Any<CancellationToken>())
            .Returns(new NotificationsSettings()
            {
                UserId = userId,
                Locale = SupportedLocales.English,
                PushesEnabled = false,
                EmailsEnabled = true
            }
            );

        await manager.SendUserInvited(userId);

        await pushStrategy.DidNotReceive().SendUserInvited(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await emailStrategy.Received(1).SendUserInvited(userId, Arg.Any<CancellationToken>());
    }
}