using Microsoft.AspNetCore.Identity;
using Security.Domain.Aggregates.User;

namespace Security.Application.Services;

public record AuthResult(User User, string? AccessToken, string? RefreshToken);

public interface IAuthenticationService
{
    public Task<AuthResult> SigninAsync(
        string email,
        string password);

    public Task<AuthResult> ConfirmEmailAsync(string userId, string token);
    public Task<AuthResult> RenewTokensAsync(string refreshToken);
    public Task LogoutAsync();
}