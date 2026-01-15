using Npgsql;
using Serilog;

namespace API.Web.Extensions;

public static class PostgresAwaiter
{
    public static void AwaitStartup(string dbConnectionString, int timeoutSeconds)
    {
        const string databaseNameConnectionParameter = "Database";
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