using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecurityModule.Contracts.Events;
using SecurityModule.Domain.Exceptions;
using SecurityModule.Domain.Services;
using SecurityModule.Application.Configs;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.Repositories;
using SecurityModule.Domain.ValueObjects;
using SecurityModule.Infrastructure.Service;
using Serilog;
using SharedCore.Exceptions;
using SharedCore.Services.LifeTimes;
using Wolverine;

namespace SecurityModule.Application.Services;

internal class AuthenticationService : IAuthenticationService, IScoped
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly AuthTokenConfig _authTokenConfig;
    private readonly IMessageBus _bus;
    private readonly JwtSecurityTokenHandler _jwtHandler;
    private readonly IEmailExistenceChecker _emailExistenceChecker;

    public AuthenticationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IOptions<AuthTokenConfig> authTokenConfigOptions,
        IMessageBus bus,
        IEmailExistenceChecker emailExistenceChecker)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _authTokenConfig = authTokenConfigOptions.Value;
        _bus = bus;
        _emailExistenceChecker = emailExistenceChecker;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    public Task<bool> IsUserExistAsync(string email)
    {
        return _userRepository.IsUserExistAsync(email);
    }

    public async Task<AuthResult> SignupAsync(string email, string password, UserType userType)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user != null && user.EmailConfirmed)
        {
            throw SecurityExceptions.UserAlreadyExists(email);
        }

        if (user == null)
        {
            var exists = await _emailExistenceChecker.CheckDomainExists(email);
            if (!exists)
            {
                throw SecurityExceptions.EmailDoesNotExist(email);
            }

            user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                Type = userType,
                RequiredAction = RequiredAction.EmailVerification,
            };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                throw SecurityExceptions.InternalError(string.Join(";", errors));
            }
        }

        var confirmationCode = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        await _bus.PublishAsync(
            new AccountCreatedEvent
            {
                UserId = user.Id,
                Email = email,
                ConfirmationCode = confirmationCode,
                Locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return new AuthResult(
            User: user,
            AccessToken: null,
            RefreshToken: null);
    }

    public async Task<AuthResult> SigninAsync(string email, string password)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            throw SecurityExceptions.UserNotFound(email);
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw SecurityExceptions.UserNotFound(email);
        }

        if (!user.EmailConfirmed)
        {
            return new AuthResult(
                User: user,
                AccessToken: null,
                RefreshToken: null);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user: user,
            password: password,
            lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                throw SecurityExceptions.LockedOut(
                    "Your account has been locked for 5 minutes. Please try again later.");
            }
            throw SecurityExceptions.WrongCredentials("Invalid email or password");
        }

        var (accessToken, refreshToken) = await GenerateTokensAsync(user);

        return new AuthResult(
            User: user,
            AccessToken: accessToken,
            RefreshToken: refreshToken);
    }

    public async Task<AuthResult> SigninExternalAsync(string email, UserType userType)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Type = userType,
                RequiredAction = RequiredAction.OnboardingCompletion,
                HasGoogleAuth = true,
            };

            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                throw SecurityExceptions.InternalError(string.Join(";", errors));
            }

            // In fact, it's not necessary to send this event here,
            // but we publish it for consistency
            await _bus.PublishAsync(
                new AccountCreatedEvent
                {
                    UserId = user.Id,
                    Email = email,
                    ConfirmationCode = null,
                    Locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                }
            );
        }
        else
        {
            var shouldSave = !user.HasGoogleAuth || !user.EmailConfirmed;

            if (!user.HasGoogleAuth)
            {
                user.HasGoogleAuth = true;
            }

            // If user exists but email not confirmed,
            // we can confirm it here since they logged in with a trusted provider
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                if (user.RequiredAction == RequiredAction.EmailVerification)
                {
                    user.RequiredAction = RequiredAction.OnboardingCompletion;
                }
            }

            if (shouldSave)
            {
                await _userManager.UpdateAsync(user);
            }
        }

        var (accessToken, refreshToken) = await GenerateTokensAsync(user);

        return new AuthResult(
            User: user,
            AccessToken: accessToken,
            RefreshToken: refreshToken);
    }

    public async Task<AuthResult> ConfirmEmailAsync(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw SecurityExceptions.UserNotFound(userId);
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var message = result.Errors.FirstOrDefault()?.Description ?? "Unknown Error";
            throw SecurityExceptions.InvalidConfirmationToken(message);
        }

        user.RequiredAction = RequiredAction.OnboardingCompletion;
        await _userManager.UpdateAsync(user);

        var (accessToken, refreshToken) = await GenerateTokensAsync(user);
        return new AuthResult(
            User: user,
            AccessToken: accessToken,
            RefreshToken: refreshToken);
    }

    public async Task<AuthResult> RenewTokensAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw SecurityExceptions.UnauthorizedAccess("Refresh token is missing");
        }

        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (storedToken == null || !storedToken.IsActive)
        {
            throw SecurityExceptions.UnauthorizedAccess("Invalid or revoked access token");
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user == null)
        {
            throw SecurityExceptions.UserNotFound(storedToken.UserId.ToString());
        }

        _refreshTokenRepository.Delete(storedToken);

        var (newAccessToken, newRefreshToken) = await GenerateTokensAsync(user);

        return new AuthResult(
            User: user,
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken);
    }

    public async Task LogoutAsync()
    {
        var claimsPrincipal = _signInManager.Context.User;
        var jti = claimsPrincipal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (jti == null)
        {
            Log.Error("No JTI found for user {0}", claimsPrincipal.Identity?.Name);
        }
        else
        {
            await _refreshTokenRepository.DeleteByJtiAsync(jti);
            await _refreshTokenRepository.SaveChangesAsync();
        }
    }

    public async Task RequestPasswordResetAsync(string email, string baseUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Don't reveal that user doesn't exist for security reasons
            return;
        }

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        await _bus.PublishAsync(
            new PasswordResetRequestedEvent
            {
                UserId = user.Id,
                Email = email,
                BaseUrl = baseUrl,
                ResetToken = resetToken,
            }
        );
    }

    public async Task ConfirmPasswordResetAsync(string email, string token, string newPassword)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw SecurityExceptions.UserNotFound(email);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            var message = result.Errors.FirstOrDefault()?.Description ?? "Password reset failed";
            throw CommonExceptions.InternalError(message);
        }
    }

    public async Task DeleteAccountAsync(string userId)
    {
        await LogoutAsync();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw SecurityExceptions.UserNotFound(userId);
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var message = result.Errors.FirstOrDefault()?.Description ?? "Account deletion failed";
            throw CommonExceptions.InternalError(message);
        }

        await _bus.PublishAsync(new AccountDeletedEvent { Id = user.Id });
    }

    private async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(User user)
    {
        var jti = Guid.CreateVersion7();
        var accessToken = GenerateJwt(
            jti: jti,
            user: user,
            durationInDays: _authTokenConfig.AccessTokenDurationInDays);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
            user: user,
            jti: jti,
            durationInDays: _authTokenConfig.RefreshTokenDurationInDays,
            maxSimultaneousLoggedInDevices: _authTokenConfig.MaxSimultaneousLoggedInDevices);
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();
        return (accessToken, refreshToken.Token);
    }

    private string GenerateJwt(Guid jti, User user, int durationInDays)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
            new Claim(AuthClaims.UserType, user.Type.ToString()),
            new Claim(ClaimTypes.Role, AuthRoles.Admin),
            // It must be used on load balancer to choose the proper user's data center.
            // But currently we have only one region
            new Claim(AuthClaims.Region, "eu")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authTokenConfig.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authTokenConfig.Issuer,
            audience: _authTokenConfig.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(durationInDays),
            signingCredentials: creds);
        return _jwtHandler.WriteToken(token);
    }
}