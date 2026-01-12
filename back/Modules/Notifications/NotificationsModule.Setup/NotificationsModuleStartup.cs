using System.Text;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NotificationsModule.Infrastructure.Persistence;
using Shared.Setup;
using Microsoft.Extensions.Configuration;
using NotificationsModule.Infrastructure.Transport.Emails.Configs;

namespace NotificationsModule.Setup;

public class NotificationsModuleStartup : AppModuleStartupBase<NotificationsContext>
{
    public NotificationsModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Application.AssemblyMarker).Assembly,
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ]
        )
    {
    }

    public override void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        base.Register(appBuilder, host);
        RegisterEmailConfig(appBuilder);
        RegisterFirebase(appBuilder);

        appBuilder.Services.AutoRegisterAssemblies(ModuleAssemblies);
    }

    private static void RegisterEmailConfig(IHostApplicationBuilder appBuilder)
    {
        var authTokenConfigSection = appBuilder.Configuration
            .GetSection($"Notifications:{nameof(EmailConfig)}");
        appBuilder.Services.AddOptions<EmailConfig>()
            .Bind(authTokenConfigSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static void RegisterFirebase(IHostApplicationBuilder appBuilder)
    {
        var base64Config = appBuilder.Configuration["Notifications:FirebaseConfig"];
        var data = Convert.FromBase64String(base64Config!);
        var jsonConfig = Encoding.UTF8.GetString(data);

        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromJson(jsonConfig)
        });
    }
}