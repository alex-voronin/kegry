using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Shared.Setup;
using UserProfiles.Infrastructure.Persistence;
using UserProfiles.Domain.Aggregates;
using UserProfiles.API;

namespace UserProfiles.Setup;

public class UserProfilesModuleStartup : AppModuleStartupBase<UserProfilesContext>
{
    public UserProfilesModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(UserProfilesContext).Assembly,
                typeof(UserProfile).Assembly,
                typeof(UserProfileEndpoints).Assembly
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
