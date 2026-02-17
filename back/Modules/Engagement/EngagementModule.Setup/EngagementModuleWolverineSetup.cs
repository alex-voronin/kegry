using EngagementModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(EngagementModuleWolverineSetup))]

namespace EngagementModule.Setup;

public class EngagementModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(Application.AssemblyMarker).Assembly);
    }
}