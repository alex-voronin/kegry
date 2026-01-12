using LocationModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(LocationModuleWolverineSetup))]

namespace LocationModule.Setup;

public class LocationModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(Application.AssemblyMarker).Assembly);
    }
}