using API.Web.Exceptions;
using JasperFx.CodeGeneration;
using JasperFx.CodeGeneration.Model;
using JasperFx.Resources;
using Notifications.Setup;
using Npgsql;
using Security.Setup;
using Serilog;
using Shared.Infrastructure.Interfaces;
using Shared.Setup;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Postgresql;

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
            const string databaseNameConnectionParameter = "Database";
            var dbConnectionString = builder.Configuration.GetConnectionString(SharedConstants.DbName)!;
            var connectionParams = ConnectionStringToDictionary(dbConnectionString);
            var postgresConnectionString = string.Join(";",
                connectionParams
                    .Where(x => x.Key != databaseNameConnectionParameter)
                    .Select(x => x.Key + "=" + x.Value)
            );

            var elapsedSeconds = 0;
            while (elapsedSeconds < timeoutSeconds)
            {
                try
                {
                    using var conn = new NpgsqlConnection(postgresConnectionString);
                    conn.Open();
                    Log.Information("PostgreSQL is up and running!");
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, $"Elapsed {elapsedSeconds + 1} seconds. PostgreSQL not available yet.");
                    Thread.Sleep(1000);
                    elapsedSeconds++;
                }
            }

            throw new InvalidOperationException($"PostgreSQL is not available after {elapsedSeconds} seconds.");
        }

        public void UseWolverineInMemoryMessageBus()
        {
            builder.UseWolverine(opts =>
                {
                    var dbConnectionString = builder.Configuration.GetConnectionString(SharedConstants.DbName)!;
                    opts.PersistMessagesWithPostgresql(dbConnectionString);

                    opts.Policies.UseDurableLocalQueues();
                    opts.Durability.MessageStorageSchemaName = "wolverine";
                    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;

                    opts.Discovery.DisableConventionalDiscovery();
                    opts.Discovery.CustomizeHandlerDiscovery(x =>
                    {
                        x.Includes.WithAttribute<WolverineHandlerAttribute>();
                    });

                    opts.Include<SecurityModuleWolverineSetup>();
                    opts.Include<NotificationsModuleWolverineSetup>();
                }
            );
            builder.Host.UseResourceSetupOnStartup();
        }
    }

    private static Dictionary<string, string> ConnectionStringToDictionary(string dbConnectionString)
    {
        var parts = dbConnectionString
            .Split(';')
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
        return parts.Select(x => x.Split("=", 2))
            .ToDictionary(x => x[0], x => x[1]);
    }
}