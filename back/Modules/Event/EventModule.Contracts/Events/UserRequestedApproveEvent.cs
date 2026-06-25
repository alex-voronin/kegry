using SharedCore.Events;

namespace EventModule.Contracts.Events;

public record UserRequestedApproveEvent : IdempotentEventBase
{
    public Guid EventCreatorId { get; init; }
    public Guid EventId { get; init; }
    public Guid EventTitle { get; init; }
    public DateTimeOffset EventStartDate { get; init; }
    public Guid JoinedUserId { get; init; }
    public override Guid IdempotencyKey() => EventCreatorId;
}