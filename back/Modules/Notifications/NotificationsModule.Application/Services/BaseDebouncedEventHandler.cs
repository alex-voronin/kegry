using Microsoft.Extensions.Caching.Hybrid;
using SharedCore.Events;
using Wolverine;

namespace NotificationsModule.Application.Services;

internal class EventContext
{
    public Guid Id { get; }
    public DateTimeOffset LastSentAt { get; }
    public bool IsScheduledAfterDebounce { get; set; }

    public EventContext(Guid id, DateTimeOffset lastSentAt)
    {
        Id = id;
        LastSentAt = lastSentAt;
    }
}

public record ScheduledEventMessage<T>(T Event);

public interface IDebouncedEventHandler<TEvent> where TEvent : UserEvent
{
    // subclasses must implement these methods to allow wolverine auto-discover the handlers
    Task HandleAsync(TEvent @event, CancellationToken ct);
    Task HandleAsync(ScheduledEventMessage<TEvent> message, CancellationToken ct);
}

public abstract class BaseDebouncedEventHandler<TEvent> : IDebouncedEventHandler<TEvent> where TEvent : UserEvent
{
    private readonly int _debounceDurationSeconds = 180;
    private readonly TimeProvider _timeProvider;
    private readonly HybridCache _cache;
    private readonly IMessageBus _messageBus;
    private readonly HybridCacheEntryOptions _cacheTtlOptions;

    public BaseDebouncedEventHandler(TimeProvider timeProvider, HybridCache cache, IMessageBus messageBus)
    {
        _timeProvider = timeProvider;
        _cache = cache;
        _messageBus = messageBus;
        _cacheTtlOptions = new HybridCacheEntryOptions()
        {
            Expiration = TimeSpan.FromSeconds(_debounceDurationSeconds),
            LocalCacheExpiration = TimeSpan.FromSeconds(_debounceDurationSeconds),
        };
    }

    protected async Task DebounceEventAsync(TEvent @event, CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(@event.UserId);

        var newContext = BuildContext();
        var context = await _cache.GetOrCreateAsync<EventContext>(
            key: cacheKey,
            factory: async _ => await Task.FromResult(newContext),
            options: _cacheTtlOptions,
            cancellationToken: ct);

        var isNewContext = context.Id == newContext.Id;
        if (isNewContext)
        {
            await InternalHandleEventAsync(@event, ct);
        }
        else
        {
            if (!context.IsScheduledAfterDebounce)
            {
                var scheduledTime = context.LastSentAt.AddSeconds(_debounceDurationSeconds);
                await _messageBus.ScheduleAsync(new ScheduledEventMessage<TEvent>(@event), scheduledTime);
                // TODO: this leads to a race condition which can be ignored for now.
                // We can use a distributed lock here to fix it
                context.IsScheduledAfterDebounce = true;
                await _cache.SetAsync(key: cacheKey, value: context, cancellationToken: ct);
            }
        }
    }

    protected async Task HandleScheduledMessageAsync(
        ScheduledEventMessage<TEvent> message,
        CancellationToken ct = default)
    {
        var cacheKey = BuildCacheKey(message.Event.UserId);
        await _cache.SetAsync(
            key: cacheKey,
            value: BuildContext(),
            options:_cacheTtlOptions,
            cancellationToken: ct);
        await InternalHandleEventAsync(message.Event, ct);
    }
    
    private EventContext BuildContext() => new EventContext(Guid.NewGuid(), _timeProvider.GetUtcNow());

    private static string BuildCacheKey(Guid userId) => $"event:context:{typeof(TEvent)}:{userId}";

    protected abstract Task InternalHandleEventAsync(TEvent @event, CancellationToken ct);
    
    public abstract Task HandleAsync(TEvent @event, CancellationToken ct);
    public abstract Task HandleAsync(ScheduledEventMessage<TEvent> message, CancellationToken ct);
}