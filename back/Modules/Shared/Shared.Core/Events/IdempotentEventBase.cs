namespace SharedCore.Events;

public abstract class IdempotentEventBase
{
    public abstract Guid IdempotencyKey();
}