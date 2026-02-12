using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using PostHog;
using Shared.Infrastructure.Interfaces;

namespace AnalyticsModule.Setup;

public class AnalyticsModuleStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.AddPostHog();
    }

    public void Initialize(WebApplication app)
    {
    }
}