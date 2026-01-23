namespace ChatModule.API.Dto;

public record GetChatResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool ParticipantsMet,
    UserDto Participant,
    List<ChatMessageDto> Messages
);
