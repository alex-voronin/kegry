using LocationModule.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shared.Setup;

namespace LocationModule.Setup;

public class LocationModuleStartup : AppModuleStartupBase<LocationContext>
{
    public LocationModuleStartup(IConfigurationManager configuration) : 
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
