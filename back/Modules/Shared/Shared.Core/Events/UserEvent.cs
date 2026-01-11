namespace SharedCore.Events;

public class UserEvent : IdempotentEventBase
{
    public required Guid UserId { get; init; }

    public override Guid IdempotencyKey() => UserId;
}