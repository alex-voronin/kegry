using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationsModule.Infrastructure;
using NotificationsModule.Infrastructure.Transport.Email;
using NotificationsModule.Infrastructure.Transport.Email.Configs;
using Shared.Infrastructure.Interfaces;
using Shared.Setup;

namespace NotificationsModule.Setup;

public class NotificationsModuleStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        var authTokenConfigSection = appBuilder.Configuration
            .GetSection($"Notifications:{nameof(EmailConfig)}");
        appBuilder.Services.AddOptions<EmailConfig>()
            .Bind(authTokenConfigSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        Assembly[] moduleAssemblies =
        [
            typeof(NotificationsModule.Application.AssemblyMarker).Assembly,
            typeof(AssemblyMarker).Assembly
        ];
        appBuilder.Services.AutoRegisterAssemblies(moduleAssemblies);
    }

    public void Initialize(WebApplication app)
    {
    }
}