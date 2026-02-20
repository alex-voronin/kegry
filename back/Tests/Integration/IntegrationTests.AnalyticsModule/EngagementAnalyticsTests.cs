using IntegrationTests.Shared;
using PostHog;
using NSubstitute;
using AnalyticsModule.Application;
using EngagementModule.Contracts.Events;
using LocationModule.Contracts.Events;
using Wolverine.Tracking;

namespace IntegrationTests.AnalyticsModule;

[Collection("IntegrationTests")]
public class EngagementAnalyticsTests : IntegrationTestBase, IAsyncLifetime
{
    public EngagementAnalyticsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task WhenUserInvitesNpc_ThenNpcInvitedEventIsCaptured()
    {
        var npcId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var @event = new NpcInvitedEvent(NpcId: npcId, UserId: userId);

        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);

        Factory.PostHogClient.Received(1).Capture(
            distinctId: userId.ToString(),
            eventName: AnalyticsEvents.Engagement.NpcInvited,
            properties: Arg.Is<Dictionary<string, object>>(p =>
                p[AnalyticsEvents.Keys.NpcId].ToString() == npcId.ToString()
            )
        );
    }
}