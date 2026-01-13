namespace ChatModule.API.Dto;

public record GetChatResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    bool ParticipantsMet,
    UserDto Participant,
    List<ChatMessageDto> Messages
);
