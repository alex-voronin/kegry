using System.Diagnostics.CodeAnalysis;
using AutoFixture.Xunit2;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Time.Testing;
using NotificationsModule.Application.Services;
using NSubstitute;
using SharedCore.Events;
using Shouldly;
using Tests.Shared.AutoFixture;
using Wolverine;

namespace Tests.NotificationsModule.Application;

public class BaseDebouncedEventHandlerTests
{
    public class TestEvent : UserEvent
    {
        [SetsRequiredMembers]
        public TestEvent(Guid userId)
        {
            UserId = userId;
        }
    }

    public class TestDebouncedEventHandler : BaseDebouncedEventHandler<TestEvent>
    {
        public bool HandleDebouncedEventCalled { get; private set; }
        public TestEvent? LastHandledEvent { get; private set; }

        public TestDebouncedEventHandler(TimeProvider timeProvider, HybridCache cache, IMessageBus messageBus) 
            : base(timeProvider, cache, messageBus)
        {
        }

        protected override Task HandleDebouncedEventAsync(TestEvent @event, CancellationToken ct)
        {
            HandleDebouncedEventCalled = true;
            LastHandledEvent = @event;
            return Task.CompletedTask;
        }

        public override Task HandleAsync(TestEvent @event, CancellationToken ct)
        {
            return DebounceEventAsync(@event, ct);
        }

        public override Task HandleAsync(ScheduledEventMessage<TestEvent> message, CancellationToken ct)
        {
            return HandleScheduledMessageAsync(message, ct);
        }
    }

    [Theory, AutoNSubstituteData]
    public async Task HandleAsync_WhenNoCacheEntry_ThenCreatesContextAndHandlesEvent(
        [Frozen] HybridCache cache,
        [Frozen] IMessageBus messageBus,
        TestDebouncedEventHandler handler,
        Guid userId)
    {
        var @event = new TestEvent(userId);
        var expectedCacheKey = TestDebouncedEventHandler.BuildCacheKey(userId);
        
        cache.GetOrCreateAsync(
                expectedCacheKey, 
                Arg.Any<Func<CancellationToken, ValueTask<EventContext>>>(),
                Arg.Any<Func<Func<CancellationToken, ValueTask<EventContext>>, CancellationToken, ValueTask<EventContext>>>(), // Factory
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => 
            {
                var state = callInfo.ArgAt<Func<CancellationToken, ValueTask<EventContext>>>(1);
                var factory = callInfo.ArgAt<Func<Func<CancellationToken, ValueTask<EventContext>>, CancellationToken, ValueTask<EventContext>>>(2);
                return factory(state, CancellationToken.None);
            });
        
        await handler.HandleAsync(@event, CancellationToken.None);
        
        handler.HandleDebouncedEventCalled.ShouldBeTrue();
        handler.LastHandledEvent.ShouldBe(@event);
        await messageBus.DidNotReceiveWithAnyArgs().SendAsync(Arg.Any<object>(), Arg.Any<DeliveryOptions>());
    }

    [Theory, AutoNSubstituteData]
    public async Task HandleAsync_WhenCacheEntryExistsAndNotScheduled_ThenSchedulesMessage(
        [Frozen] HybridCache cache,
        [Frozen] IMessageBus messageBus,
        TestDebouncedEventHandler handler,
        Guid userId)
    {
        var @event = new TestEvent(userId);
        var lastSentAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var existingContext = new EventContext(Guid.NewGuid(), lastSentAt) { IsScheduledAfterDebounce = false };
        var expectedCacheKey = TestDebouncedEventHandler.BuildCacheKey(userId);
        
        cache.GetOrCreateAsync(
                expectedCacheKey, 
                Arg.Any<Func<CancellationToken, ValueTask<EventContext>>>(), 
                Arg.Any<Func<Func<CancellationToken, ValueTask<EventContext>>, CancellationToken, ValueTask<EventContext>>>(), // Factory
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<EventContext>(existingContext));
            
        messageBus.SendAsync(Arg.Any<ScheduledEventMessage<TestEvent>>(), Arg.Any<DeliveryOptions>())
           .Returns(ValueTask.CompletedTask);
        
        await handler.HandleAsync(@event, CancellationToken.None);
        
        handler.HandleDebouncedEventCalled.ShouldBeFalse();
        
        var calls = messageBus.ReceivedCalls();
        var scheduledCall = calls.FirstOrDefault(c => 
            c.GetArguments().Any(a => a is ScheduledEventMessage<TestEvent>));
        scheduledCall.ShouldNotBeNull("Expected IMessageBus to receive a call with ScheduledEventMessage");
        
        existingContext.IsScheduledAfterDebounce.ShouldBeTrue();
        await cache.Received(1).SetAsync(
            expectedCacheKey, 
            existingContext, 
            Arg.Any<HybridCacheEntryOptions>(), 
            Arg.Any<IEnumerable<string>>(), 
            Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task HandleAsync_WhenCacheEntryExistsAndAlreadyScheduled_ThenDoesNothing(
        [Frozen] HybridCache cache,
        [Frozen] IMessageBus messageBus,
        TestDebouncedEventHandler handler,
        Guid userId)
    {
        var @event = new TestEvent(userId);
        var existingContext = new EventContext(Guid.NewGuid(), DateTimeOffset.UtcNow) { IsScheduledAfterDebounce = true };
        var expectedCacheKey = TestDebouncedEventHandler.BuildCacheKey(userId);

        cache.GetOrCreateAsync(
                expectedCacheKey, 
                Arg.Any<Func<CancellationToken, ValueTask<EventContext>>>(),
                Arg.Any<Func<Func<CancellationToken, ValueTask<EventContext>>, CancellationToken, ValueTask<EventContext>>>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<EventContext>(existingContext));

        await handler.HandleAsync(@event, CancellationToken.None);
        
        handler.HandleDebouncedEventCalled.ShouldBeFalse();
        messageBus.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory, AutoNSubstituteData]
    public async Task HandleAsync_WhenCacheEntryExistsAndNotScheduled_ThenSchedulesMessageAndHandlesItAfterDelay(
        [Frozen] FakeTimeProvider timeProvider,
        [Frozen] HybridCache cache,
        [Frozen] IMessageBus messageBus,
        TestDebouncedEventHandler handler,
        Guid userId)
    {
        timeProvider.AutoAdvanceAmount = TimeSpan.Zero;
        var @event = new TestEvent(userId);
        var lastSentAt = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var existingContext = new EventContext(Guid.NewGuid(), lastSentAt) { IsScheduledAfterDebounce = false };
        var expectedCacheKey = TestDebouncedEventHandler.BuildCacheKey(userId);

        // 1. Setup cache the way we already received the event, but haven't scheduled debounce
        cache.GetOrCreateAsync(
                expectedCacheKey, 
                Arg.Any<Func<CancellationToken, ValueTask<EventContext>>>(),
                Arg.Any<Func<Func<CancellationToken, ValueTask<EventContext>>, CancellationToken, ValueTask<EventContext>>>(),
                Arg.Any<HybridCacheEntryOptions>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new ValueTask<EventContext>(existingContext));
            
        messageBus.SendAsync(Arg.Any<ScheduledEventMessage<TestEvent>>(), Arg.Any<DeliveryOptions>())
           .Returns(ValueTask.CompletedTask);

        // 2. Send the same subsequent event
        await handler.HandleAsync(@event, CancellationToken.None);
        
        handler.HandleDebouncedEventCalled.ShouldBeFalse("Event should not be handled immediately when debouncing");
        var calls = messageBus.ReceivedCalls();
        var scheduledCall = calls.FirstOrDefault(c => 
            c.GetArguments().Any(a => a is ScheduledEventMessage<TestEvent>));
        scheduledCall.ShouldNotBeNull("Message should be scheduled");
        
        var options = scheduledCall.GetArguments().OfType<DeliveryOptions>().FirstOrDefault();
        options.ShouldNotBeNull();
        
        //3. Simulate delay elapsed
        var expectedScheduledTime = lastSentAt.AddSeconds(handler.DebounceDurationSeconds);
        options.ScheduledTime.ShouldBe(expectedScheduledTime);
        timeProvider.SetUtcNow(expectedScheduledTime);
        
        //4. call scheduled message handler
        var scheduledMessage = scheduledCall.GetArguments().OfType<ScheduledEventMessage<TestEvent>>().First();
        await handler.HandleAsync(scheduledMessage, CancellationToken.None);


        handler.HandleDebouncedEventCalled.ShouldBeTrue("Event should be handled when scheduled message is processed");
        handler.LastHandledEvent.ShouldBe(@event); // Should be the same event object as in the message
        await cache.Received(1).SetAsync(
            expectedCacheKey,
            Arg.Is<EventContext>(ctx => ctx.IsScheduledAfterDebounce == false && ctx.LastSentAt == expectedScheduledTime),
            Arg.Any<HybridCacheEntryOptions>(), 
            Arg.Any<IEnumerable<string>>(), 
            Arg.Any<CancellationToken>());
    }
}
