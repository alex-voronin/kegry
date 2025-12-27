using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Setup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AutoRegisterAssemblies(this IServiceCollection services, Assembly[] assemblies)
    {
        services.Scan(scan =>
        {
            scan.FromAssemblies(assemblies)
                .AddClasses(classes => classes.Where(t => t.IsClass && t.IsPublic && t.GetInterfaces().Any()))
                .AsImplementedInterfaces()
                .WithScopedLifetime();
        });
        return services;
    }
}