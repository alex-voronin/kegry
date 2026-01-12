using ChatModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(ChatModuleWolverineSetup))]

namespace ChatModule.Setup;

public class ChatModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(Application.AssemblyMarker).Assembly);
    }
}