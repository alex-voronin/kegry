using System.Text;
using Autofac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Security.Application.Settings;
using Security.Domain.Aggregates.User;
using Security.Infrastructure.Persistence;
using Shared.Setup;

namespace Security.Setup;

public class SecurityStartup : AppModuleStartupBase<SecurityContext>
{
    public SecurityStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(Application.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ]
        )
    {
    }

    public override void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        base.Register(appBuilder, host);

        host.ConfigureContainer<ContainerBuilder>((_, containerBuilder) =>
        {
            containerBuilder.RegisterModule(new AutoRegistrationAutofacModule(ModuleAssemblies));
        });

        var jwtSection = appBuilder.Configuration.GetSection("Authentication:JwtSettings");
        appBuilder.Services.AddOptions<JwtSettings>()
            .Bind(jwtSection)
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
        var jwtSection = configuration.GetSection("Authentication:JwtSettings");
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["Secret"]!)),
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
        services.AddIdentity<User, IdentityRole<Guid>>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = true;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 12;
                o.User.RequireUniqueEmail = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<SecurityContext>()
            .AddDefaultTokenProviders();
    }
}