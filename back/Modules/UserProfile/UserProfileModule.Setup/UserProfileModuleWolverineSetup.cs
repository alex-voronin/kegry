using UserProfileModule.Setup;
using Wolverine;
using Wolverine.Attributes;

[assembly: WolverineModule(typeof(UserProfileModuleWolverineSetup))]

namespace UserProfileModule.Setup;

public class UserProfileModuleWolverineSetup : IWolverineExtension
{
    public void Configure(WolverineOptions options)
    {
    }
}