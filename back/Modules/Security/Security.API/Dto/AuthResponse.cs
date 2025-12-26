using Security.Domain.Aggregates.User;

namespace Security.API.Dto;

public class AuthResponse
{
    public required string UserId { get; init; }
    public required RequiredAction RequiredAction { get; init; }
    public required string? AccessToken { get; init; }
    public required string? RefreshToken { get; init; }
}