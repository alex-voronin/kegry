using ChatModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(ChatModuleWolverineSetup))]

namespace ChatModule.Setup;

public class ChatModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Publish(rule =>
        {
            rule.MessagesFromNamespace(typeof(Contracts.AssemblyMarker).Namespace!);
            rule.ToLocalQueue("chat-events");
        });
    }
}