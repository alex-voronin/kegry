using Shared.Infrastructure.Interfaces;
using SharedCore.Localization;

namespace API.Web.Startup;

public class LocalizationStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.Services.AddSingleton<IDateTimeHumanizer, DateTimeHumanizer>();
        appBuilder.Services.AddLocalization();
    }

    public void Initialize(WebApplication app)
    {
    }
}