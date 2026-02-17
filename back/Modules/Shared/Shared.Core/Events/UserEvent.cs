namespace SharedCore.Events;

public record UserEvent : IdempotentEventBase
{
    public required Guid UserId { get; init; }
    public bool IsDeleted { get; set; }

    public override Guid IdempotencyKey() => UserId;
}