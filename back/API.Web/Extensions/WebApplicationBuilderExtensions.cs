using API.Web.Exceptions;
using Npgsql;
using Serilog;
using Shared.Infrastructure.Interfaces;
using Shared.Setup;

namespace API.Web.Extensions;

public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        public void RegisterServices(IFeatureStartup[] startups)
        {
            foreach (var startup in startups)
            {
                startup.Register(builder, builder.Host);
            }

            builder.Services.AddExceptionHandler<ExceptionHandler>();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddOpenApi();
        }

        public void AwaitDatabaseStartup(int timeoutSeconds)
        {
            var dbConnectionString = builder.Configuration.GetConnectionString(SharedConstants.DbName)!;
            PostgresAwaiter.AwaitStartup(dbConnectionString, timeoutSeconds);
        }
    }
}