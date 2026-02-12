using IntegrationTests.Shared;
using PostHog;
using NSubstitute;
using AnalyticsModule.Application;
using SecurityModule.Contracts.Events;
using Wolverine.Tracking;

namespace IntegrationTests.AnalyticsModule;

[Collection("IntegrationTests")]
public class AccountAnalyticsTests : IntegrationTestBase, IAsyncLifetime
{
    public AccountAnalyticsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task WhenUserSignsUpAndConfirmsEmail_ThenAccountCreatedEventIsCaptured()
    {
        var @event = new AccountCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            ConfirmationCode = "123456"
        };
        
        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);
        
        Factory.PostHogClient.Received(1).Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.Account.Created,
            properties: Arg.Is<Dictionary<string, object>>(p => p["email"].ToString() == @event.Email)
        );
    }

    [Fact]
    public async Task WhenAccountIsDeleted_ThenAccountDeletedEventIsCaptured()
    {
        var @event = new AccountDeletedEvent
        {
            Id = Guid.NewGuid()
        };
        
        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);
        
        Factory.PostHogClient.Received(1).Capture(
            distinctId: @event.Id.ToString(),
            eventName: AnalyticsEvents.Account.Deleted
        );
    }

    [Fact]
    public async Task WhenPasswordResetIsRequested_ThenPasswordResetRequestedEventIsCaptured()
    {
        var @event = new PasswordResetRequestedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "reset@example.com",
            ResetToken = "token"
        };
        
        await Factory.Host
            .TrackActivity()
            .DoNotAssertOnExceptionsDetected()
            .PublishMessageAndWaitAsync(@event);
        
        Factory.PostHogClient.Received(1).Capture(
            distinctId: @event.UserId.ToString(),
            eventName: AnalyticsEvents.Account.PasswordResetRequested,
            properties: Arg.Is<Dictionary<string, object>>(p => p["email"].ToString() == @event.Email)
        );
    }
}
