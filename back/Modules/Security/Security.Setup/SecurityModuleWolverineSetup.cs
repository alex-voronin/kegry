using Wolverine;

namespace Security.Setup;

public class SecurityModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
        options.Publish(rule =>
        {
            rule.MessagesFromNamespace(typeof(SecurityModuleStartup).Namespace!);
            rule.ToLocalQueue("account-events");
        });
    }
}