using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Shared.Setup;
using UserProfileModule.Infrastructure.Persistence;

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
    }
}
