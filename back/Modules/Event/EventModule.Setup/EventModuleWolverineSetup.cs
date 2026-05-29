using Wolverine;
using EventModule.Application;
using EventModule.Setup;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(EventModuleWolverineSetup))]

namespace EventModule.Setup;

public class EventModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(AssemblyMarker).Assembly);
    }
}
