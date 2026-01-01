using FeedModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Shared.Setup;


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
    
    protected override void SetupEfCoreExtensions(NpgsqlDbContextOptionsBuilder npgsqlOptions)
    {
        npgsqlOptions.UseNetTopologySuite();
    }
}
