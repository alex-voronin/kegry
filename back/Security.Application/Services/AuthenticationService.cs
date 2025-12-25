using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Settings;
using Security.Domain.Aggregates.RefreshToken;
using Security.Domain.Aggregates.User;
using Security.Domain.Exceptions;
using Security.Domain.Services;
using Serilog;
using SharedCore.Exceptions;

namespace Security.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private record LoginResult(User User, string AccessToken, string RefreshToken);
    private record SignupResult(User User, string ConfirmationToken);
    
    private const int DefaultJwtDurationInDays = 30;

    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _jwtHandler;

    public AuthenticationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtSettings> jwtSettingsOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtSettings = jwtSettingsOptions.Value;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    public async Task<SigninResult> SigninAsync(string email, string password)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            var signupResult = await Signup(email, password);
            // TODO: emit event for Notification service
            return new SigninResult(
                User: signupResult.User, 
                AccessToken: null, 
                RefreshToken: null);
        }
        else
        {
            var loginResult = await Login(email, password);
            return new SigninResult(
                User: loginResult.User, 
                AccessToken: loginResult.AccessToken, 
                RefreshToken: loginResult.RefreshToken);
        }
    }

    public async Task<User> ConfirmEmailAsync(long userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw SecurityExceptions.UserNotFound(userId);
        }
        
        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var message = result.Errors.FirstOrDefault()?.Description ?? "Unknown Error";
            throw CommonExceptions.InternalError(message);
        }

        user.RequiredAction = RequiredAction.OnboardingCompletion;
        await _userManager.UpdateAsync(user);
        
        await _signInManager.SignInAsync(user: user, isPersistent: true);
        return user;
    }

    public async Task<RenewTokensResult> RenewTokensAsync(string refreshToken)
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

        return new RenewTokensResult(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken
        );
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

        await _signInManager.SignOutAsync();
    }

    private async Task<SignupResult> Signup(
        string email,
        string password)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user != null && user.EmailConfirmed)
        {
            throw SecurityExceptions.UserAlreadyExists(email);
        }

        if (user == null)
        {
            user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = email,
                Email = email,
                RequiredAction = RequiredAction.EmailVerification,
            };
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToArray();
                throw SecurityExceptions.InternalError(string.Join(";", errors));
            }
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return new SignupResult(
            User: user,
            ConfirmationToken: token
        );
    }

    private async Task<LoginResult> Login(
        string email,
        string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw SecurityExceptions.UserNotFound(email);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user: user,
            password: password,
            isPersistent: true,
            lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw CommonExceptions.InternalError(result.ToString());
        }

        var (accessToken, refreshToken) = await GenerateTokensAsync(user);

        return new LoginResult(
            User: user,
            AccessToken: accessToken,
            RefreshToken: refreshToken
        );
    }

    private async Task<(string accessToken, string refreshToken)> GenerateTokensAsync(User user)
    {
        var jti = Guid.NewGuid();
        var accessToken = GenerateJwt(jti: jti, user: user, durationInDays: DefaultJwtDurationInDays);
        var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user, jti);
        await _refreshTokenRepository.AddAsync(refreshToken);
        await _refreshTokenRepository.SaveChangesAsync();
        return (accessToken, accessToken);
    }

    private string GenerateJwt(Guid jti, User user, int durationInDays)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.Now.AddDays(durationInDays),
            signingCredentials: creds);
        return _jwtHandler.WriteToken(token);
    }
}