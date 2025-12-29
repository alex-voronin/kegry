using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Shared.Setup;
using UserProfiles.Infrastructure.Persistence;

namespace UserProfiles.Setup;

public class UserProfilesModuleStartup : AppModuleStartupBase<UserProfilesContext>
{
    public UserProfilesModuleStartup(IConfigurationManager configuration) :
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

    public override void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        base.Register(appBuilder, host);
        appBuilder.Services.AutoRegisterAssemblies(ModuleAssemblies);
    }
}
