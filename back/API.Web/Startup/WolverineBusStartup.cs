using JasperFx.Resources;
using Notifications.Setup;
using Security.Setup;
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
        appBuilder.UseWolverine(opts =>
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

                opts.Include<SecurityModuleWolverineSetup>();
                opts.Include<NotificationsModuleWolverineSetup>();
            }
        );
        host.UseResourceSetupOnStartup();
    }

    public void Initialize(WebApplication app)
    {
    }
}