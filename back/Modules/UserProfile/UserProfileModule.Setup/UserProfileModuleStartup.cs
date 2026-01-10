using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using UserProfileModule.Infrastructure.Persistence;
using Shared.Setup;

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
