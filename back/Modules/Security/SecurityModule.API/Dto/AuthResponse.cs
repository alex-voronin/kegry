using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.ValueObjects;

namespace SecurityModule.API.Dto;

public record AuthResponse(
    string UserId,
    UserType UserType,
    RequiredAction RequiredAction,
    string? AccessToken,
    string? RefreshToken
);