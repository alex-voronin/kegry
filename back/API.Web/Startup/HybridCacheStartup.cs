using Microsoft.Extensions.Caching.Hybrid;
using Shared.Infrastructure.Interfaces;

namespace API.Web.Startup;

public class HybridCacheStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.Services.AddHybridCache(options => 
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            };
        });
    }

    public void Initialize(WebApplication app)
    {
    }
}