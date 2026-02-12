using IntegrationTests.Shared;
using PostHog;
using NSubstitute;
using AnalyticsModule.Application;
using LocationModule.Contracts.Events;
using Wolverine.Tracking;

namespace IntegrationTests.AnalyticsModule;

[Collection("IntegrationTests")]
public class LocationAnalyticsTests : IntegrationTestBase, IAsyncLifetime
{
    public LocationAnalyticsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task WhenUsersMetByLocation_ThenUsersMetByLocationEventIsCaptured()
    {
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var @event = new TrackedUsersMetEvent
        {
            UserIds = [user1, user2]
        };
        
        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);
        
        Factory.PostHogClient.Received(1).Capture(
            distinctId: user1.ToString(),
            eventName: AnalyticsEvents.Location.UsersMet,
            properties: Arg.Is<Dictionary<string, object>>(p => 
                p["other_user_id"].ToString() == user2.ToString())
        );
    }
}
