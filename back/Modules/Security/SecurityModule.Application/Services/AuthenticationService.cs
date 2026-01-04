using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SecurityModule.Contracts.Events;
using SecurityModule.Domain.Aggregates.RefreshToken;
using SecurityModule.Domain.Aggregates.User;
using SecurityModule.Domain.Exceptions;
using SecurityModule.Domain.Services;
using SecurityModule.Application.Configs;
using Serilog;
using SharedCore.Exceptions;
using SharedCore.Services.LifeTimes;
using Wolverine;

namespace SecurityModule.Application.Services;

internal class AuthenticationService : IAuthenticationService, IScoped
{
    private record LoginResult(User User, string AccessToken, string RefreshToken);

    private record SignupResult(User User, string ConfirmationCode);

    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AuthTokenConfig _authTokenConfig;
    private readonly IMessageBus _bus;
    private readonly JwtSecurityTokenHandler _jwtHandler;

    public AuthenticationService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IRefreshTokenService refreshTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<AuthTokenConfig> authTokenConfigOptions,
        IMessageBus bus)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _refreshTokenService = refreshTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _authTokenConfig = authTokenConfigOptions.Value;
        _bus = bus;
        _jwtHandler = new JwtSecurityTokenHandler();
    }

    public async Task<AuthResult> SigninAsync(string email, string password)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            var signupResult = await Signup(email, password);
            await _bus.PublishAsync(
                new AccountCreatedEvent
                {
                    UserId = signupResult.User.Id,
                    Email = email,
                    ConfirmationCode = signupResult.ConfirmationCode
                }
            );
            return new AuthResult(
                User: signupResult.User,
                AccessToken: null,
                RefreshToken: null);
        }
        
        var loginResult = await Login(email, password);
        return new AuthResult(
            User: loginResult.User,
            AccessToken: loginResult.AccessToken,
            RefreshToken: loginResult.RefreshToken);
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
            throw CommonExceptions.InternalError(message);
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
            ConfirmationCode: token
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

        var result = await _signInManager.CheckPasswordSignInAsync(
            user: user,
            password: password,
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
        return (accessToken, accessToken);
    }

    private string GenerateJwt(Guid jti, User user, int durationInDays)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.UserName!),
            new Claim(JwtRegisteredClaimNames.Jti, jti.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authTokenConfig.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _authTokenConfig.Issuer,
            audience: _authTokenConfig.Audience,
            claims: claims,
            expires: DateTime.Now.AddDays(durationInDays),
            signingCredentials: creds);
        return _jwtHandler.WriteToken(token);
    }
}