using System.Reflection;
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
            discovery: ExtensionDiscovery.ManualOnly, 
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
                
                LoadWolverineExtensions(opts);
            }
        );
        host.UseResourceSetupOnStartup();
    }

    public void Initialize(WebApplication app)
    {
    }

    private void LoadWolverineExtensions(WolverineOptions options)
    {
        var assemblies = FindExtensionAssemblies();
        foreach (var assembly in assemblies)
        {
            var extensionTypes = FindExtensionTypes(assembly);
            foreach (var extensionType in extensionTypes)
            {
                var extension = (IWolverineExtension)Activator.CreateInstance(extensionType)!;   
                options.Include(extension);
            }
        }
    }

    private static IEnumerable<Assembly> FindExtensionAssemblies()
    {
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(assembly => assembly.GetCustomAttribute<WolverineModuleAttribute>() != null);
        return assemblies;
    }
    
    private static IEnumerable<Type> FindExtensionTypes(Assembly assembly)
    {
        var extensionTypes = assembly.GetTypes()
            .Where(p => typeof(IWolverineExtension).IsAssignableFrom(p) 
                        && p.IsClass 
                        && !p.IsAbstract 
                        && p.GetConstructor(Type.EmptyTypes) != null);
        return extensionTypes;
    }
}