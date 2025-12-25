using System.Reflection;
using Autofac;
using Module = Autofac.Module;

namespace Shared.Setup;

public class AutoRegistrationAutofacModule : Module
{
    private readonly Assembly[] _assembliesToScan;

    public AutoRegistrationAutofacModule(Assembly[] assembliesToScan)
    {
        _assembliesToScan = assembliesToScan;
    }
    
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(_assembliesToScan)
            .Where(t => t.IsClass && t.IsPublic && t.GetInterfaces().Any())
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
    }
}