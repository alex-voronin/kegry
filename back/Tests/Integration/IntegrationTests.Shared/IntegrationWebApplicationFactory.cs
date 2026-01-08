using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Shared.Managers;
using JasperFx.CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Respawn;
using Xunit;

namespace IntegrationTests.Shared;

public class IntegrationWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DbUser = "postgres";
    private const string DbPassword = "admin";
    private const string DbName = "hoppyfydb";
    private const int DbHostPort = 54321;
    private const int DbContainerPort = 5432;

    private static readonly string DbConnectionString = $"Host=localhost;Port={DbHostPort};Username={DbUser};" +
                                                        $"Password={DbPassword};Database={DbName}";

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;
    public IntegrationApiClient ApiClient { get; private set; } = null!;
    public AuthManager AuthManager { get; private set; } = null!;

    private readonly IContainer _dbContainer =
        new ContainerBuilder("kartoza/postgis:18-3.6--v2025.11.24")
            .WithEnvironment("POSTGRES_USER", DbUser)
            .WithEnvironment("POSTGRES_PASSWORD", DbPassword)
            .WithEnvironment("POSTGRES_DB", DbName)
            .WithPortBinding(DbHostPort, DbContainerPort)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(DbContainerPort))
            .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => { logging.AddConsole(); });

        // connection string must be configured via builder.UseSetting as 
        // AppModuleStartupBase reads connection string in constructor
        builder.UseSetting("ConnectionStrings:hoppyfydb", DbConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:AuthTokenConfig:Secret"] = "super-secret-key-that-is-at-least-32-chars-long!",
                ["Authentication:AuthTokenConfig:Issuer"] = "hoppyfy.me",
                ["Authentication:AuthTokenConfig:Audience"] = "hoppyfy.me",
                ["Authentication:AuthTokenConfig:AccessTokenDurationInDays"] = "1",
                ["Authentication:AuthTokenConfig:RefreshTokenDurationInDays"] = "7",
                ["Authentication:AuthTokenConfig:MaxSimultaneousLoggedInDevices"] = "5"
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Without this flag WebHost terminates immediately after startup
        JasperFxEnvironment.AutoStartHost = true;

        return base.CreateHost(builder);
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        var httpClient = CreateClient();

        ApiClient = new IntegrationApiClient(httpClient);
        AuthManager = new AuthManager(ApiClient, Services);

        await InitializeRespawnerAsync(DbConnectionString);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();

        await _dbConnection.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    private async Task InitializeRespawnerAsync(string dbConnectionString)
    {
        _dbConnection = new NpgsqlConnection(dbConnectionString);
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            SchemasToInclude = ["feed", "security", "users", "location", "notifications", "public"],
            DbAdapter = DbAdapter.Postgres
        });
    }
}