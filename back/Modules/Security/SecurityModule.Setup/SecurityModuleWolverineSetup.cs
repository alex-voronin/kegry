using SecurityModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(SecurityModuleWolverineSetup))]

namespace SecurityModule.Setup;

public class SecurityModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Publish(rule =>
        {
            rule.MessagesFromNamespace(typeof(Contracts.AssemblyMarker).Namespace!);
            rule.ToLocalQueue("account-events");
        });
    }
}