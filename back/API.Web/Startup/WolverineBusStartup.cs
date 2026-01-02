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
            }
        );
        host.UseResourceSetupOnStartup();
    }

    public void Initialize(WebApplication app)
    {
    }
}