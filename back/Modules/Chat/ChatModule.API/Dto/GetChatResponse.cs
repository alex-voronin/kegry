namespace ChatModule.API.Dto;

public record GetChatResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    UserDto Participant,
    List<ChatMessageDto> Messages
);
