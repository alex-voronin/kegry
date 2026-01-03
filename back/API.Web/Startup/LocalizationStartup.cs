using Shared.Infrastructure.Interfaces;

namespace API.Web.Startup;

public class LocalizationStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.Services.AddLocalization();
    }

    public void Initialize(WebApplication app)
    {
    }
}