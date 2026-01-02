using NotificationsModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(NotificationsModuleWolverineSetup))]

namespace NotificationsModule.Setup;

public class NotificationsModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(Application.AssemblyMarker).Assembly);
    }
}