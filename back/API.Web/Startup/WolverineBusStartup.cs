using JasperFx.CodeGeneration;
using JasperFx.Resources;
using Shared.Infrastructure.Interfaces;
using Shared.Setup;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Postgresql;

namespace API.Web.Startup;

public class WolverineBusStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        host.UseWolverine(
            discovery: ExtensionDiscovery.Automatic,
            overrides: opts =>
            {
                opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;

                opts.Durability.HealthCheckPollingTime = TimeSpan.FromSeconds(30);
                opts.Durability.NodeReassignmentPollingTime = TimeSpan.FromSeconds(30);
                opts.Durability.ScheduledJobPollingTime = TimeSpan.FromSeconds(10);

                var dbConnectionString = appBuilder.Configuration.GetConnectionString(SharedConstants.DbName)!;
                opts.PersistMessagesWithPostgresql(dbConnectionString);

                opts.Policies.UseDurableLocalQueues();
                opts.Durability.MessageStorageSchemaName = "wolverine";
                opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

                opts.Discovery.DisableConventionalDiscovery();
                opts.Discovery.CustomizeHandlerDiscovery(x =>
                {
                    x.Includes.WithAttribute<WolverineHandlerAttribute>();
                });

                opts.Policies.MessageExecutionLogLevel(LogLevel.Information);
            }
        );
        host.UseResourceSetupOnStartup();
    }

    public void Initialize(WebApplication app)
    {
    }
}