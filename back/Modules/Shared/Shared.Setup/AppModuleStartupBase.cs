using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Interfaces;
using Shared.Infrastructure.Persistence;
using SharedCore.Api;

namespace Shared.Setup;

public abstract class AppModuleStartupBase<TContext> : IFeatureStartup where TContext : DbContext
{
    private readonly string _dbConnectionString;
    protected readonly Assembly[] ModuleAssemblies;

    public AppModuleStartupBase(IConfigurationManager configuration, params Assembly[] moduleAssemblies)
    {
        _dbConnectionString = configuration.GetConnectionString(SharedConstants.DbName)!;
        ModuleAssemblies = moduleAssemblies;
    }

    public virtual void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        RegisterEfCoreContexts(appBuilder.Services, _dbConnectionString);
        appBuilder.Services.AutoRegisterAssemblies(ModuleAssemblies);
    }

    public virtual void Initialize(WebApplication app)
    {
        RunEfCoreMigrations(app.Services.CreateScope());
        MapModuleEndpoints(app, ModuleAssemblies);
    }

    protected virtual void RegisterEfCoreContexts(IServiceCollection services, string dbConnectionString)
    {
        services.AddDbContext<TContext>(options =>
            options.UseNpgsql(dbConnectionString)
                .UseSnakeCaseNamingConvention()
        );
    }

    private void RunEfCoreMigrations(IServiceScope scope)
    {
        using (scope)
        {
            var context = scope.ServiceProvider.GetRequiredService<TContext>();
            if (context is ReadonlyDbContext)
            {
                context.Database.EnsureCreated();
            }
            else
            {
                context.Database.Migrate();
            }
        }
    }

    private static void MapModuleEndpoints(WebApplication app, Assembly[] assemblies)
    {
        var endpointTypes = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.IsAssignableTo(typeof(IEndpoints)));

        foreach (var type in endpointTypes)
        {
            var method = type.GetMethod(nameof(IEndpoints.MapEndpoints),
                BindingFlags.Static | BindingFlags.Public);
            method?.Invoke(null, [app]);
        }
    }
}