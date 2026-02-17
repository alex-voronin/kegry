using EngagementModule.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Shared.Setup;

namespace EngagementModule.Setup;

public class EngagementModuleStartup : AppModuleStartupBase<EngagementContext>
{
    public EngagementModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(Application.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ]
        )
    {
    }
}
