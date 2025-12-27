using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Notifications.Infrastructure.Transport.Email;
using Shared.Infrastructure.Interfaces;
using Shared.Setup;

namespace Notifications.Setup;

public class NotificationsModuleStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.Services
            .AddHttpClient<MailgunEmailSender>()
            .AddStandardResilienceHandler();
        
        var authTokenConfigSection = appBuilder.Configuration
            .GetSection($"Notifications:{nameof(EmailConfig)}");
        appBuilder.Services.AddOptions<EmailConfig>()
            .Bind(authTokenConfigSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        Assembly[] moduleAssemblies =
        [
            typeof(Application.AssemblyMarker).Assembly,
            typeof(Infrastructure.AssemblyMarker).Assembly
        ];
        appBuilder.Services.AutoRegisterAssemblies(moduleAssemblies);
    }

    public void Initialize(WebApplication app)
    {
    }
}