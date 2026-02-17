using IntegrationTests.Shared;
using PostHog;
using NSubstitute;
using AnalyticsModule.Application;
using FeedModule.Contracts.Events;
using Shouldly;
using Wolverine.Tracking;

namespace IntegrationTests.AnalyticsModule;

[Collection("IntegrationTests")]
public class FeedAnalyticsTests : IntegrationTestBase, IAsyncLifetime
{
    public FeedAnalyticsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task WhenUserInvited_ThenUserInvitedEventIsCaptured()
    {
        var @event = new UserInvitedEvent
        {
            UserId = Guid.NewGuid(),
            ByUserId = Guid.NewGuid(),
            InviteExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };

        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);

        Factory.PostHogClient.Received(1).Capture(
            distinctId: @event.ByUserId.ToString(),
            eventName: AnalyticsEvents.Feed.UserInvited,
            properties: Arg.Is<Dictionary<string, object>>(p =>
                p[AnalyticsEvents.Keys.OtherUserId].ToString() == @event.UserId.ToString() &&
                p[AnalyticsEvents.Keys.ExpiresAt].ToString() == @event.InviteExpiresAt.ToString("o") &&
                p[AnalyticsEvents.Keys.Environment].ToString() == AnalyticsEnv.Dev)
        );
    }

    [Fact]
    public async Task WhenUserBlocked_ThenUserBlockedEventIsCaptured()
    {
        var @event = new UserBlockedEvent
        {
            UserId = Guid.NewGuid(),
            ByUserId = Guid.NewGuid(),
        };

        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);

        Factory.PostHogClient.Received(1).Capture(
            distinctId: @event.ByUserId.ToString(),
            eventName: AnalyticsEvents.Feed.UserInvited,
            properties: Arg.Is<Dictionary<string, object>>(p =>
                p[AnalyticsEvents.Keys.OtherUserId].ToString() == @event.UserId.ToString() &&
                p[AnalyticsEvents.Keys.Environment].ToString() == AnalyticsEnv.Dev)
        );
    }

    [Fact]
    public async Task WhenMeetingArranged_ThenMeetingArrangedEventIsCaptured()
    {
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        var @event = new MeetingArrangedEvent
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Participants =
            [
                new MeetingArrangedEvent.Participant(userAId, "User 1"),
                new MeetingArrangedEvent.Participant(userBId, "User 2")
            ]
        };

        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);

        Factory.PostHogClient.Received(1).Capture(
            distinctId: userAId.ToString(),
            eventName: AnalyticsEvents.Feed.MeetingArranged,
            properties: Arg.Any<Dictionary<string, object>>()
        );

        // hack, but seems it's the only way to check ParticipantIds array content
        var call = Factory.PostHogClient.ReceivedCalls()
            .Last(c => c.GetMethodInfo().Name == "Capture");
        var propsArg = call.GetArguments()[2];
        propsArg.ShouldNotBeNull();
        var props = (Dictionary<string, object>)propsArg;
        props[AnalyticsEvents.Keys.ExpiresAt].ShouldBe(@event.ExpiresAt.ToString("o"));
        props[AnalyticsEvents.Keys.Environment].ShouldBe(AnalyticsEnv.Dev);
        props[AnalyticsEvents.Keys.ParticipantIds].ShouldBeEquivalentTo(
            new[] { userAId.ToString(), userBId.ToString() }
            );
    }

    [Fact]
    public async Task WhenMeetingEnded_ThenMeetingEndedEventIsCaptured()
    {
        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();
        var @event = new MeetingsEndedEvent
        {
            ParticipantIdsFromDeletedChats = [[userAId, userBId]]
        };

        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);

        Factory.PostHogClient.Received(1).Capture(
            distinctId: userAId.ToString(),
            eventName: AnalyticsEvents.Feed.MeetingEnded,
            properties: Arg.Any<Dictionary<string, object>>()
        );

        // hack, but seems it's the only way to check ParticipantIds array content
        var call = Factory.PostHogClient.ReceivedCalls()
            .Last(c => c.GetMethodInfo().Name == "Capture");
        var propsArg = call.GetArguments()[2];
        propsArg.ShouldNotBeNull();
        var props = (Dictionary<string, object>)propsArg;
        props[AnalyticsEvents.Keys.Environment].ShouldBe(AnalyticsEnv.Dev);
        props[AnalyticsEvents.Keys.ParticipantIds].ShouldBeEquivalentTo(new[] { userAId.ToString(), userBId.ToString() });
    }
}