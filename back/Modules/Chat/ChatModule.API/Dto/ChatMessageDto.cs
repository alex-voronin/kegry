namespace ChatModule.API.Dto;

public record ChatMessageDto(
    Guid Id,
    string MessageText,
    DateTimeOffset CreatedAt,
    bool IsMine
);
