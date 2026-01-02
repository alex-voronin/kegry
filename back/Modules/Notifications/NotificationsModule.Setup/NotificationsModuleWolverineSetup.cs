using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule]

namespace NotificationsModule.Setup;

public class NotificationsModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(Application.AssemblyMarker).Assembly);
    }
}