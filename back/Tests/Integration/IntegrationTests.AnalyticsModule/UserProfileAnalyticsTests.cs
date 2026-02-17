using IntegrationTests.Shared;
using PostHog;
using NSubstitute;
using AnalyticsModule.Application;
using UserProfileModule.Contracts.Events;
using Wolverine.Tracking;

namespace IntegrationTests.AnalyticsModule;

[Collection("IntegrationTests")]
public class UserProfileAnalyticsTests : IntegrationTestBase, IAsyncLifetime
{
    public UserProfileAnalyticsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task WhenProfileCreated_ThenUserProfileCreatedEventIsCaptured()
    {
        var @event = new UserProfileChangedEvent
        {
            UserId = Guid.NewGuid(),
            IsNewProfile = true,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            AvatarImageS3Key = "avatar.png"
        };

        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);

        await Factory.PostHogClient.Received(1).IdentifyAsync(
            distinctId: Arg.Is(@event.UserId.ToString()),
            personPropertiesToSet: Arg.Is<Dictionary<string, object>>(p =>
                p["email"].ToString() == @event.Email &&
                p["name"].ToString() == $"{@event.FirstName} {@event.LastName}" &&
                p[AnalyticsEvents.Keys.Environment].ToString() == AnalyticsEnv.Dev),
            personPropertiesToSetOnce: Arg.Is<Dictionary<string, object>?>(x => x == null),
            cancellationToken: Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task WhenProfileUpdated_ThenUserProfileUpdatedEventIsCaptured()
    {
        var @event = new UserProfileChangedEvent
        {
            UserId = Guid.NewGuid(),
            IsNewProfile = false,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            AvatarImageS3Key = "avatar.png"
        };

        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);

        Factory.PostHogClient.Received(1).Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.UserProfile.Updated,
            properties: Arg.Is<Dictionary<string, object>>(p =>
                p[AnalyticsEvents.Keys.Environment].ToString() == AnalyticsEnv.Dev)
        );
    }
}
