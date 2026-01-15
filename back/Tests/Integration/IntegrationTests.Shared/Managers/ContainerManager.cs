using System.Data.Common;
using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Xunit;

namespace IntegrationTests.Shared.Managers;

public class ContainerManager : IAsyncLifetime
{
    internal static readonly string DbConnectionString = $"Host=localhost;" +
                                                         $"Port={TestConfig.Db.HostPort};" +
                                                         $"Username={TestConfig.Db.User};" +
                                                         $"Password={TestConfig.Db.Password};" +
                                                         $"Database={TestConfig.Db.DbName}";

    private DbConnection? _dbConnection;
    private Respawner _respawner = null!;

    private readonly IContainer _dbContainer =
        new ContainerBuilder("kartoza/postgis:18-3.6--v2025.11.24")
            .WithEnvironment("POSTGRES_USER", TestConfig.Db.User)
            .WithEnvironment("POSTGRES_PASSWORD", TestConfig.Db.Password)
            .WithEnvironment("POSTGRES_DB", TestConfig.Db.DbName)
            .WithPortBinding(TestConfig.Db.HostPort, TestConfig.Db.ContainerPort)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(TestConfig.Db.ContainerPort))
            .Build();

    private readonly IContainer _minioContainer =
        new ContainerBuilder("minio/minio")
            .WithCommand("server", "/data")
            .WithEnvironment("MINIO_ROOT_USER", TestConfig.S3.AccessKey)
            .WithEnvironment("MINIO_ROOT_PASSWORD", TestConfig.S3.SecretKey)
            .WithPortBinding(TestConfig.S3.HostPort, TestConfig.S3.ContainerPort)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(TestConfig.S3.HostPort))
            .Build();

    public async Task ResetDatabaseAsync()
    {
        if (_dbConnection != null)
        {
            await _respawner.ResetAsync(_dbConnection);
        }
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await _minioContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync();
        }

        await _dbContainer.DisposeAsync();
        await _minioContainer.DisposeAsync();
    }

    internal async Task CreateTestBucketAsync()
    {
        var s3Config = new AmazonS3Config
        {
            ServiceURL = TestConfig.S3.ServiceUrl,
            ForcePathStyle = true
        };

        var policyJson = $@"
            {{
                ""Version"": ""2012-10-17"",
                ""Statement"": [
                    {{
                        ""Effect"": ""Allow"",
                        ""Principal"": {{ ""AWS"": [""*""] }},
                        ""Action"": [""s3:GetObject""],
                        ""Resource"": [""arn:aws:s3:::{TestConfig.S3.BucketName}/*""]
                    }}
                ]
            }}";

        using var client = new AmazonS3Client(TestConfig.S3.AccessKey, TestConfig.S3.SecretKey, s3Config);
        await client.PutBucketAsync(new PutBucketRequest { BucketName = TestConfig.S3.BucketName });
        await client.PutBucketPolicyAsync(new PutBucketPolicyRequest
        {
            BucketName = TestConfig.S3.BucketName,
            Policy = policyJson
        });
    }

    internal async Task InitializeRespawnerAsync()
    {
        _dbConnection = new NpgsqlConnection(DbConnectionString);
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["feed", "security", "user_profile", "location", "notifications", "wolverine"],
            TablesToIgnore = [new Table(schema: "user_profile", name: "hobbies")]
        });
    }
}