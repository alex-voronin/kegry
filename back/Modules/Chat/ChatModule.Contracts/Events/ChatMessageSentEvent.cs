using SharedCore.Events;

namespace ChatModule.Contracts.Events;

public record ChatMessageSentEvent : IdempotentEventBase
{
    public required Guid MessageId { get; init; }
    public required Guid ToUserId { get; init; }
    public required Guid FromUserId { get; init; }
    public required string FromUserName { get; init; }
    public required string FromUserAvatarUrl { get; init; }

    public required string MessageText { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public override Guid IdempotencyKey() => ToUserId;
}