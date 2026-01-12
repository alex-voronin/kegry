using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedCore.Interfaces;
using SharedCore.Services.LifeTimes;

namespace Shared.Setup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AutoRegisterAssemblies(this IServiceCollection services, Assembly[] assemblies)
    {
        services.Scan(scan =>
        {
            scan.FromAssemblies(assemblies)
                .AddClasses(c => c.AssignableTo<ITransient>(), publicOnly: false)
                .AsImplementedInterfaces()
                .WithTransientLifetime()
                .AddClasses(c => c.AssignableToAny(typeof(IScoped), typeof(IRepository)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(c => c.AssignableTo<ISingleton>(), publicOnly: false)
                .AsImplementedInterfaces()
                .WithSingletonLifetime()
                .AddClasses(c => c.AssignableTo<BackgroundService>(), publicOnly: false)
                .AsImplementedInterfaces()
                .WithSingletonLifetime();
        });
        return services;
    }
}