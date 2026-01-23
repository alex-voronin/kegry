namespace ChatModule.API.Dto;

public record GetChatResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int MaxMessagesPerUser,
    bool ParticipantsMet,
    UserDto Participant,
    List<ChatMessageDto> Messages
);
