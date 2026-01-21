using LocationModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Shared.Setup;
using Wolverine.EntityFrameworkCore;

namespace LocationModule.Setup;

public class LocationModuleStartup : AppModuleStartupBase<LocationContext>
{
    public LocationModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(Application.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ]
        )
    {
    }

    protected override void RegisterEfCoreContexts(IServiceCollection services, string dbConnectionString)
    {
        services.AddDbContextWithWolverineIntegration<LocationContext>(options =>
            options
                .UseNpgsql(dbConnectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite())
                .UseSnakeCaseNamingConvention()
        );
    }
}
