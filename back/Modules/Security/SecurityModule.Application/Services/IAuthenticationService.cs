using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.ValueObjects;

namespace SecurityModule.Application.Services;

public record AuthResult(User User, string? AccessToken, string? RefreshToken);

public interface IAuthenticationService
{
    public Task<AuthResult> SigninAsync(
        string email,
        string password);

    public Task<AuthResult> SignupAsync(
        string email,
        string password,
        UserType userType);

    public Task<AuthResult> SigninExternalAsync(string email, UserType userType);

    public Task<bool> IsUserExistAsync(string email);
    public Task<AuthResult> ConfirmEmailAsync(string userId, string token);
    public Task<AuthResult> RenewTokensAsync(string refreshToken);
    public Task LogoutAsync(string? deviceId = null);

    public Task ResendConfirmationEmailAsync(string userId);
    public Task RequestPasswordResetAsync(string email, string baseUrl);
    public Task ConfirmPasswordResetAsync(string email, string token, string newPassword);
    public Task DeleteAccountAsync(string userId);
}