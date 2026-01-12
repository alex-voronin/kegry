using SecurityModule.Domain.Aggregates;

namespace SecurityModule.Application.Services;

public record AuthResult(User User, string? AccessToken, string? RefreshToken);

public interface IAuthenticationService
{
    public Task<AuthResult> SigninAsync(
        string email,
        string password);

    public Task<AuthResult> SignupAsync(
        string email,
        string password);

    public Task<bool> IsUserExistAsync(string email);
    public Task<AuthResult> ConfirmEmailAsync(string userId, string token);
    public Task<AuthResult> RenewTokensAsync(string refreshToken);
    public Task LogoutAsync();
    
    public Task RequestPasswordResetAsync(string email);
    public Task ConfirmPasswordResetAsync(string userId, string token, string newPassword);
}