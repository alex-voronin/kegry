using SharedCore.Validation;

namespace ChatModule.Domain.Aggregates;

public class ChatMessage
{
    public required Guid Id { get; init; }
    public required Guid ChatId { get; init; }
    public required Guid FromUserId { get; init; }

    public required string MessageText
    {
        get;
        init => field = ValidateMessageText(
            fieldName: nameof(MessageText),
            value: value);
    }
    public required DateTimeOffset CreatedAt { get; init; }

    public Chat Chat { get; init; } = null!;

    internal ChatMessage()
    {
    }

    private static string ValidateMessageText(string fieldName, string value)
    {
        Validation.ThrowIfNullOrWhiteSpace(fieldName, value);
        Validation.ThrowIfLengthExceeded(fieldName, value, ValidationParameters.MaxMessageLength);
        return value.Trim();
    }
}