using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using PostHog;
using Shared.Infrastructure.Interfaces;
using Shared.Setup;

namespace AnalyticsModule.Setup;

public class AnalyticsModuleStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.AddPostHog();
        appBuilder.Services.AutoRegisterAssemblies([typeof(Application.AssemblyMarker).Assembly]);
    }

    public void Initialize(WebApplication app)
    {
    }
}