using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserProfileModule.Infrastructure.Persistence;
using Shared.Setup;
using Wolverine.EntityFrameworkCore;

namespace UserProfileModule.Setup;

public class UserProfileModuleStartup : AppModuleStartupBase<UserProfileContext>
{
    public UserProfileModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(Application.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ]
        )
    {
    }

    public override void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        base.Register(appBuilder, host);
        appBuilder.Services.AutoRegisterAssemblies(ModuleAssemblies);
    }

    protected override void RegisterEfCoreContexts(IServiceCollection services, string dbConnectionString)
    {
        services.AddDbContextWithWolverineIntegration<UserProfileContext>(options =>
            options
                .UseNpgsql(dbConnectionString)
                .UseSnakeCaseNamingConvention()
        );
    }
}
