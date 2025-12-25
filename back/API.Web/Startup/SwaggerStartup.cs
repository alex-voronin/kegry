using Microsoft.OpenApi;
using Shared.Infrastructure.Interfaces;

namespace API.Web.Startup;

public class SwaggerStartup : IFeatureStartup
{
    public void Register(IHostApplicationBuilder appBuilder, IHostBuilder host)
    {
        appBuilder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "hoppyfy.me API",
                Version = "v1",
            });
        });
    }

    public void Initialize(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "hoppyfy.me API");
            c.RoutePrefix = string.Empty;
        });
    }
}