using SecurityModule.Domain.Aggregates.User;

namespace SecurityModule.API.Dto;

public record AuthResponse(
    string UserId,
    RequiredAction RequiredAction,
    string? AccessToken,
    string? RefreshToken
);