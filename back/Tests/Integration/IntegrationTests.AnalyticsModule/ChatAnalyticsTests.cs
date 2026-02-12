using IntegrationTests.Shared;
using PostHog;
using NSubstitute;
using AnalyticsModule.Application;
using ChatModule.Contracts.Events;
using Wolverine.Tracking;

namespace IntegrationTests.AnalyticsModule;

[Collection("IntegrationTests")]
public class ChatAnalyticsTests : IntegrationTestBase, IAsyncLifetime
{
    public ChatAnalyticsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task WhenChatMessageIsSent_ThenMessageSentEventIsCaptured()
    {
        var @event = new ChatMessageSentEvent
        {
            ToUserId = Guid.NewGuid(),
            FromUserId = Guid.NewGuid(),
            FromUserName = "User",
            FromUserAvatarUrl = "http://avatar",
            MessageText = "Hello",
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);
        
        Factory.PostHogClient.Received(1).Capture(
            distinctId: @event.FromUserId.ToString(),
            eventName: AnalyticsEvents.Chat.MessageSent
        );
    }
}
