using SecurityModule.Domain.Aggregates.User;

namespace SecurityModule.API.Dto;

public class AuthResponse
{
    public required string UserId { get; init; }
    public required RequiredAction RequiredAction { get; init; }
    public required string? AccessToken { get; init; }
    public required string? RefreshToken { get; init; }
}