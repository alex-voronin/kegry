using Microsoft.Extensions.Configuration;
using Shared.Setup;
using VenueModule.Infrastructure.Persistence;

namespace VenueModule.Setup;

public class VenueModuleStartup : AppModuleStartupBase<VenueContext>
{
    public VenueModuleStartup(IConfigurationManager configuration) :
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
}