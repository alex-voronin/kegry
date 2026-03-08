namespace SharedCore.Events;

public record UserEvent : IdempotentEventBase
{
    public required Guid UserId { get; init; }

    public override Guid IdempotencyKey() => UserId;
}