using SharedCore.Events;

namespace EventModule.Contracts.Events;

public record UserParticipationApprovedEvent : IdempotentEventBase
{
    public required Guid EventId { get; init; }
    public required string EventTitle { get; init; } = null!;
    public required DateTimeOffset EventStartDate { get; init; }

    public required Guid EventCreatorId { get; init; }
    public required Guid JoinedUserId { get; init; }
    public override Guid IdempotencyKey() => JoinedUserId;
}