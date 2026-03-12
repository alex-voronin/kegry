using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Setup;
using VenueModule.Infrastructure.Persistence;
using Wolverine.EntityFrameworkCore;

namespace VenueModule.Setup;

public class VenueModuleStartup : AppModuleStartupBase<VenueContext>
{
    public VenueModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ]
        )
    {
    }

    protected override void RegisterEfCoreContexts(IServiceCollection services, string dbConnectionString)
    {
        services.AddDbContextWithWolverineIntegration<VenueContext>(options =>
            options
                .UseNpgsql(dbConnectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite())
                .UseSnakeCaseNamingConvention()
        );
    }
}