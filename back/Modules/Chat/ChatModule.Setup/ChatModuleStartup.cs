using ChatModule.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Setup;
using Wolverine.EntityFrameworkCore;

namespace ChatModule.Setup;

public class ChatModuleStartup : AppModuleStartupBase<ChatContext>
{
    public ChatModuleStartup(IConfigurationManager configuration) :
        base(configuration: configuration,
            moduleAssemblies:
            [
                typeof(Infrastructure.AssemblyMarker).Assembly,
                typeof(Domain.AssemblyMarker).Assembly,
                typeof(Application.AssemblyMarker).Assembly,
                typeof(API.AssemblyMarker).Assembly
            ]
        )
    {
    }
}
