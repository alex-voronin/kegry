using Wolverine;

namespace NotificationsModule.Setup;

public class NotificationsModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(NotificationsModule.Application.AssemblyMarker).Assembly);
    }
}