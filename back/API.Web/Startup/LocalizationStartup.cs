using System.Globalization;
using Microsoft.AspNetCore.Localization;
using NotificationModule.Domain.ValueObjects;
using Shared.Infrastructure.Interfaces;
using SharedCore.Localization;

namespace API.Web.Startup;

public class LocalizationStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.Services.AddSingleton<IDateTimeHumanizer, DateTimeHumanizer>();
        appBuilder.Services.AddLocalization();
        
        appBuilder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
                {
                    SupportedLocales.English, "en-US", SupportedLocales.Serbian, "sr-RS"
                }
                .Select(c => new CultureInfo(c))
                .ToList();
            
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            // If we can't determine a user's culture, fallback to English
            options.SetDefaultCulture(SupportedLocales.English);
            options.DefaultRequestCulture = new RequestCulture(SupportedLocales.English);
        });
    }

    public void Initialize(WebApplication app)
    {
        app.UseRequestLocalization();
    }
}