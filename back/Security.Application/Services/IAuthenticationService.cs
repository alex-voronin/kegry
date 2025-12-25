using Microsoft.AspNetCore.Identity;
using Security.Domain.Aggregates.User;

namespace Security.Application.Services;

public record SigninResult(User User, string? AccessToken, string? RefreshToken);

public record RenewTokensResult(string AccessToken, string RefreshToken);

public interface IAuthenticationService
{
    public Task<SigninResult> SigninAsync(
        string email,
        string password);

    public Task<User> ConfirmEmailAsync(long userId, string token);
    public Task<RenewTokensResult> RenewTokensAsync(string refreshToken);
    public Task LogoutAsync();
}