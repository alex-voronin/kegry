namespace SharedCore.Events;

public abstract record IdempotentEventBase
{
    public abstract Guid IdempotencyKey();
}