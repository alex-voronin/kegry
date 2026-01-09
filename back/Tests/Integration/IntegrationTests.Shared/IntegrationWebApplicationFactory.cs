using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Shared.Managers;
using JasperFx.CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationsModule.Infrastructure.Transport.Emails;
using NotificationsModule.Infrastructure.Transport.Push;
using Npgsql;
using NSubstitute;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace IntegrationTests.Shared;

public class IntegrationWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string DbConnectionString = $"Host=localhost;" +
                                                        $"Port={TestConfig.Db.HostPort};" +
                                                        $"Username={TestConfig.Db.User};" +
                                                        $"Password={TestConfig.Db.Password};" +
                                                        $"Database={TestConfig.Db.DbName}";

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public IntegrationApiClient ApiClient { get; private set; } = null!;
    public AuthManager AuthManager { get; private set; } = null!;
    public NotificationManager NotificationManager { get; private set; } = null!;

    private readonly IContainer _dbContainer =
        new ContainerBuilder("kartoza/postgis:18-3.6--v2025.11.24")
            .WithEnvironment("POSTGRES_USER", TestConfig.Db.User)
            .WithEnvironment("POSTGRES_PASSWORD", TestConfig.Db.Password)
            .WithEnvironment("POSTGRES_DB", TestConfig.Db.DbName)
            .WithPortBinding(TestConfig.Db.HostPort, TestConfig.Db.ContainerPort)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(TestConfig.Db.ContainerPort))
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
                ["Authentication:AuthTokenConfig:Secret"] = TestConfig.AuthToken.Secret,
                ["Authentication:AuthTokenConfig:Issuer"] = TestConfig.AuthToken.Issuer,
                ["Authentication:AuthTokenConfig:Audience"] = TestConfig.AuthToken.Audience,
                ["Authentication:AuthTokenConfig:AccessTokenDurationInDays"] =
                    TestConfig.AuthToken.AccessTokenDurationInDays.ToString(),
                ["Authentication:AuthTokenConfig:RefreshTokenDurationInDays"] =
                    TestConfig.AuthToken.RefreshTokenDurationInDays.ToString(),
                ["Authentication:AuthTokenConfig:MaxSimultaneousLoggedInDevices"] =
                    TestConfig.AuthToken.MaxSimultaneousLoggedInDevices.ToString(),
                ["Storage:S3Config:AccessKey"] = TestConfig.S3.AccessKey,
                ["Storage:S3Config:SecretKey"] = TestConfig.S3.SecretKey,
                ["Storage:S3Config:ServiceUrl"] = TestConfig.S3.ServiceUrl,
                ["Storage:S3Config:PublicUrl"] = TestConfig.S3.PublicUrl,
                ["Storage:S3Config:BucketName"] = TestConfig.S3.BucketName,
                ["Storage:S3Config:KeySalt"] = TestConfig.S3.SecretKey
            });
        });

        // mocks for notification transports 
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            var emailSender = Substitute.For<IEmailSender>();
            services.AddSingleton(emailSender);

            services.RemoveAll<IPushSender>();
            var pushSender = Substitute.For<IPushSender>();
            services.AddSingleton(pushSender);

            NotificationManager = new NotificationManager(emailSender: emailSender, pushSender: pushSender);
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
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["feed", "security", "users", "location", "notifications", "wolverine"],
            TablesToIgnore = [new Table(schema: "users", name: "hobbies")]
        });
    }
}