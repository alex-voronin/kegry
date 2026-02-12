using IntegrationTests.Shared;
using PostHog;
using NSubstitute;
using AnalyticsModule.Application;
using FeedModule.Contracts.Events;
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
            MeetingExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
        };
        
        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);
        
        Factory.PostHogClient.Received(1).Capture(
            distinctId: @event.ByUserId.ToString(),
            eventName: AnalyticsEvents.Feed.UserInvited,
            properties: Arg.Is<Dictionary<string, object>>(p => 
                p["other_user_id"].ToString() == @event.UserId.ToString() &&
                p["expires_at"].ToString() == @event.MeetingExpiresAt.ToString("o"))
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
            Participants = [
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
            properties: Arg.Is<Dictionary<string, object>>(p => 
                p["expires_at"].ToString() == @event.ExpiresAt.ToString("o") &&
                p["other_user_id"].ToString() == userBId.ToString())
        );
    }

    [Fact]
    public async Task WhenMeetingEnded_ThenMeetingEndedEventIsCaptured()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var @event = new MeetingsEndedEvent
        {
            ParticipantIdsFromDeletedChats = [[user1, user2]]
        };
        
        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);
        
        Factory.PostHogClient.Received(1).Capture(
            distinctId: user1.ToString(),
            eventName: AnalyticsEvents.Feed.MeetingEnded,
            properties: Arg.Is<Dictionary<string, object>>(p => 
                p["other_user_id"].ToString() == user2.ToString())
        );
    }
}
