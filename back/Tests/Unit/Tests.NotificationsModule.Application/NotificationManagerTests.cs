using AutoFixture.Xunit2;
using NotificationModule.Domain.Aggregates;
using NotificationModule.Domain.Repositories;
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
        settingsRepository.GetNotificationsEnabledById(userId, ct: Arg.Any<CancellationToken>())
            .Returns((pushesEnabled: true, emailsEnabled: false));

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
        var settings = new NotificationsSettings { UserId = userId, PushesEnabled = false, EmailsEnabled = false };
        settingsRepository.GetByIdAsync(userId, asNoTracking: true, ct: Arg.Any<CancellationToken>())
            .Returns(settings);

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
        settingsRepository.GetNotificationsEnabledById(userId, ct: Arg.Any<CancellationToken>())
            .Returns((pushesEnabled: true, emailsEnabled: true));

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
        settingsRepository.GetNotificationsEnabledById(userId, ct: Arg.Any<CancellationToken>())
            .Returns((pushesEnabled: false, emailsEnabled: true));

        await manager.SendUserInvited(userId);

        await pushStrategy.DidNotReceive().SendUserInvited(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await emailStrategy.Received(1).SendUserInvited(userId, Arg.Any<CancellationToken>());
    }
}