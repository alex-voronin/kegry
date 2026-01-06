using FeedModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(FeedModuleWolverineSetup))]

namespace FeedModule.Setup;

public class FeedModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Publish(rule =>
        {
            rule.MessagesFromNamespace(typeof(Contracts.AssemblyMarker).Namespace!);
            rule.ToLocalQueue("feed-events");
        });
    }
}