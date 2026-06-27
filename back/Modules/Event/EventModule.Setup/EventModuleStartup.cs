using EventModule.Application.BackgroundJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Persistence.Audit;
using Shared.Setup;
using EventModule.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace EventModule.Setup;

public class EventModuleStartup : AppModuleStartupBase<EventContext>
{
    public EventModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ])
    {
    }

    protected override void RegisterEfCoreContexts(IServiceCollection services, string dbConnectionString)
    {
        services.AddDbContextWithWolverineIntegration<EventContext>(options =>
            options
                .AddInterceptors(new AuditableInterceptor())
                .UseNpgsql(dbConnectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite())
                .UseSnakeCaseNamingConvention()
        );

        services.AddHostedService<ExpiredEventsCollectorBackgroundService>();
    }
}
