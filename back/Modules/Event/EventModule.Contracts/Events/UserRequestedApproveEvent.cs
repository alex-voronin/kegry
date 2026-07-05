using SharedCore.Events;

namespace EventModule.Contracts.Events;

public record UserRequestedApproveEvent : IdempotentEventBase
{
    public required Guid EventCreatorId { get; init; }
    public required Guid EventId { get; init; }
    public required string EventTitle { get; init; }
    public required DateTimeOffset EventStartDate { get; init; }
    public required Guid JoinedUserId { get; init; }
    public override Guid IdempotencyKey() => EventCreatorId;
}