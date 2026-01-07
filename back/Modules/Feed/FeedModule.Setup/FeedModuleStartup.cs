using FeedModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Setup;
using Wolverine.EntityFrameworkCore;

namespace FeedModule.Setup;

public class FeedModuleStartup : AppModuleStartupBase<FeedContext>
{
    public FeedModuleStartup(IConfigurationManager configuration) : base(
        configuration: configuration,
        moduleAssemblies:
        [
            typeof(Infrastructure.AssemblyMarker).Assembly,
            typeof(Domain.AssemblyMarker).Assembly,
            typeof(API.AssemblyMarker).Assembly
        ])
    {
    }

    public override void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        base.Register(appBuilder, host);
        appBuilder.Services.AutoRegisterAssemblies(ModuleAssemblies);
    }
    
    protected override void RegisterEfCoreContexts(IServiceCollection services, string dbConnectionString)
    {
        services.AddDbContextWithWolverineIntegration<FeedContext>(options =>
            options
                .UseNpgsql(dbConnectionString, npgsqlOptions => npgsqlOptions.UseNetTopologySuite())
                .UseSnakeCaseNamingConvention()
        );
    }
}