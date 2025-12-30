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
                typeof(UserProfileModule.Infrastructure.AssemblyMarker).Assembly,
                typeof(UserProfileModule.Domain.AssemblyMarker).Assembly,
                typeof(UserProfileModule.API.AssemblyMarker).Assembly
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
