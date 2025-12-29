using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Shared.Setup;
using LocationModule.Infrastructure.Persistence;

namespace Location.Setup;

public class LocationModuleStartup : AppModuleStartupBase<LocationContext>
{
    public LocationModuleStartup(IConfigurationManager configuration) : 
        base(configuration: configuration, 
            moduleAssemblies: 
            [
                typeof(LocationModule.Infrastructure.AssemblyMarker).Assembly,
                typeof(LocationModule.Domain.AssemblyMarker).Assembly,
                typeof(LocationModule.API.AssemblyMarker).Assembly
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
