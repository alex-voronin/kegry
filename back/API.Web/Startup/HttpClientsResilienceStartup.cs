using Shared.Infrastructure.Interfaces;

namespace API.Web.Startup;

public class HttpClientsResilienceStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.Services.ConfigureHttpClientDefaults(httpBuilder =>
        {
            httpBuilder.AddStandardResilienceHandler();
        });
    }

    public void Initialize(WebApplication app)
    {
    }
}