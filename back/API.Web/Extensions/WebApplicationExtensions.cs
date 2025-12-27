using Domain.Common.Interfaces;
using Serilog;
using Shared.Infrastructure.Interfaces;

namespace API.Web.Extensions;

public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        public void InitializeApp(IFeatureStartup[] startups)
        {
            foreach (var startup in startups)
            {
                startup.Initialize(app);
            }

            app.UseSerilogRequestLogging();
            app.UseExceptionHandler(_ => { });
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
        }

        public void SeedData()
        {
            var scope = app.Services.CreateScope();
            using (scope)
            {
                var seedables = scope.ServiceProvider.GetServices<ISeedable>();
                foreach (var seedable in seedables)
                {
                    seedable.Seed();
                }
            }
        }
    }
}