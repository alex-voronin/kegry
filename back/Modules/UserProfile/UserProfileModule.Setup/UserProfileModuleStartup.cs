using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserProfileModule.Infrastructure.Persistence;
using Shared.Setup;
using UserProfileModule.Infrastructure.Storage.Configs;

namespace UserProfileModule.Setup;

public class UserProfileModuleStartup : AppModuleStartupBase<UserProfileContext>
{
    public UserProfileModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ]
        )
    {
    }

    public override void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        base.Register(appBuilder, host);
        appBuilder.Services.AutoRegisterAssemblies(ModuleAssemblies);

        var s3Section = appBuilder.Configuration.GetSection("Storage:S3Config");
        appBuilder.Services.AddOptions<S3Config>()
            .Bind(s3Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
