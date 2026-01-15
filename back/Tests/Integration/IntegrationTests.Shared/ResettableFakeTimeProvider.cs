using Microsoft.Extensions.Time.Testing;

namespace IntegrationTests.Shared;

public sealed class ResettableTimeProvider : TimeProvider
{
    private readonly Lock _timerLock = new Lock();
    private FakeTimeProvider _inner;
    private readonly DateTimeOffset _initial;

    public ResettableTimeProvider(DateTimeOffset initial)
    {
        _initial = initial;
        _inner = new FakeTimeProvider(initial);
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_timerLock)
        {
            return _inner.GetUtcNow();
        }
    } 

    public void Advance(TimeSpan delta)
    {
        lock (_timerLock)
        {
            _inner.Advance(delta);
        }
    }

    public void SetUtcNow(DateTimeOffset value)
    {
        lock (_timerLock)
        {
            _inner.SetUtcNow(value);
        }
    }

    public void Reset(DateTimeOffset value)
    {
        lock (_timerLock)
        {
            _inner = new FakeTimeProvider(value);
        }
    }
}