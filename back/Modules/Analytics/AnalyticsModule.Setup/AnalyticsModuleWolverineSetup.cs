using AnalyticsModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(AnalyticsModuleWolverineSetup))]

namespace AnalyticsModule.Setup;

public class AnalyticsModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(Application.AssemblyMarker).Assembly);
    }
}