namespace ChatModule.API.Dto;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? AvatarUrl
);
