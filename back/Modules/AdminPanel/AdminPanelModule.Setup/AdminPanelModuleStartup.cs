using AdminPanelModule.RazorPages.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Interfaces;

namespace AdminPanelModule.Setup;

public class AdminPanelModuleStartup : IFeatureStartup
{
    private readonly IConfigurationManager _configuration;

    public AdminPanelModuleStartup(IConfigurationManager configuration)
    {
        _configuration = configuration;
    }

    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        var configSection = _configuration.GetSection("AdminPanel");
        appBuilder.Services.Configure<AdminCredentials>(configSection);

        appBuilder.Services.AddRazorPages(options =>
        {
            options.Conventions.AddFolderRouteModelConvention("/", model =>
            {
                foreach (var selector in model.Selectors)
                {
                    if (selector.AttributeRouteModel != null)
                    {
                        var template = selector.AttributeRouteModel.Template;
                        selector.AttributeRouteModel.Template = string.IsNullOrEmpty(template)
                            ? "admin"
                            : $"admin/{template}";
                    }
                }
            });
            options.Conventions.AuthorizeFolder("/", "AdminCookiesPolicy");
            options.Conventions.AllowAnonymousToPage("/Login");
        });

        appBuilder.Services.AddAuthentication()
            .AddCookie(AdminPanelConstants.AuthScheme, options =>
            {
                options.Cookie.Name = "AdminPanel.Auth";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.LoginPath = $"{AdminPanelConstants.PathPrefix}/Login";
                options.LogoutPath = $"{AdminPanelConstants.PathPrefix}/Logout";
                options.ReturnUrlParameter = "returnUrl";
            });

        appBuilder.Services.AddAuthorizationBuilder()
            .AddPolicy("AdminCookiesPolicy", policy =>
            {
                policy.AuthenticationSchemes.Add(AdminPanelConstants.AuthScheme);
                policy.RequireAuthenticatedUser();
            });
    }

    public void Initialize(WebApplication app)
    {
        app.MapRazorPages();
    }
}
