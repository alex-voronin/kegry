using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SecurityModule.Application.Configs;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Infrastructure;
using SecurityModule.Infrastructure.Persistence;
using Shared.Setup;

namespace SecurityModule.Setup;

public class SecurityModuleStartup : AppModuleStartupBase<SecurityContext>
{
    public SecurityModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(SecurityModule.Application.AssemblyMarker).Assembly,
                typeof(SecurityModule.API.AssemblyMarker).Assembly
            ]
        )
    {
    }

    public override void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        base.Register(appBuilder, host);

        var authTokenConfigSection = appBuilder.Configuration
            .GetSection($"Authentication:{nameof(AuthTokenConfig)}");
        appBuilder.Services.AddOptions<AuthTokenConfig>()
            .Bind(authTokenConfigSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        RegisterEfCoreAsAuthenticationStore(appBuilder.Services);
        appBuilder.Services.AddAuthorization();
        RegisterAuthenticationSchemes(appBuilder.Services, appBuilder.Configuration);
    }

    private static void RegisterAuthenticationSchemes(
        IServiceCollection services,
        IConfigurationManager configuration)
    {
        var authTokenConfigSection = configuration.GetSection($"Authentication:{nameof(AuthTokenConfig)}");
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // disable automatic claims mapping to legacy names
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authTokenConfigSection["Issuer"],
                    ValidAudience = authTokenConfigSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(authTokenConfigSection["Secret"]!)
                    )
                };
            });
        // TODO: uncomment to allow Google and Apple login
        // .AddGoogle(options =>
        // {
        //     var googleAuthSection = Configuration.GetSection("Authentication:Google");
        //     options.ClientId = googleAuthSection["ClientId"];
        //     options.ClientSecret = googleAuthSection["ClientSecret"];
        //     options.CallbackPath = "/api/corporate-landing/auth/signin-google";
        //     options.AccessDeniedPath = ExternalSignInHandler.CallbackUrl;
        //     options.CorrelationCookie.Name = "sf2d.google.correlation.";
        //     options.CorrelationCookie.SecurePolicy =
        //         !HostingEnvironment.IsDevelopment()
        //             ? CookieSecurePolicy.Always
        //             : CookieSecurePolicy.None;
        // })
        // .AddApple(options =>
        // {
        //     var appleAuthSection = Configuration.GetSection("Authentication:Apple");
        //
        //     options.ClientId = appleAuthSection["ClientId"];
        //     options.KeyId = appleAuthSection["KeyId"];
        //     options.TeamId = appleAuthSection["TeamId"];
        //     options.CallbackPath = "/api/corporate-landing/auth/signin-apple";
        //     options.UsePrivateKey(keyId =>
        //         HostingEnvironment.ContentRootFileProvider.GetFileInfo($"AuthKey_{keyId}.p8"));
        //     options.CorrelationCookie.Name = "sf2d.apple.correlation.";
        //     options.CorrelationCookie.SecurePolicy =
        //         !HostingEnvironment.IsDevelopment()
        //             ? CookieSecurePolicy.Always
        //             : CookieSecurePolicy.None;
        // });
    }

    private static void RegisterEfCoreAsAuthenticationStore(IServiceCollection services)
    {
        services.AddIdentityCore<User>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 10;
                o.User.RequireUniqueEmail = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);

                o.SignIn.RequireConfirmedEmail = true;
                // 6 digits TOTP, 9 minutes lifespan. If you need change the lifespan, it must be done manually by
                // implementing your own TOTP Provider. See Microsoft.AspNetCore.Identity.EmailTokenProvider and
                // Microsoft.AspNetCore.Identity.Rfc6238AuthenticationService
                o.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<SecurityContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();
    }
}