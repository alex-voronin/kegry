using SecurityModule.Application.Configs;
using Shared.Infrastructure.Storage.Configs;

namespace IntegrationTests.Shared;

public class S3TestConfig : S3Config
{
    public required int HostPort { get; init; }
    public required int ContainerPort { get; init; }
}

public static class TestConfig
{
    public static class Db
    {
        internal const string User = "postgres";
        internal const string Password = "admin";
        internal const string DbName = "hoppyfydb";
        internal const int HostPort = 54321;
        internal const int ContainerPort = 5432;
    }

    public static readonly AuthTokenConfig AuthToken = new AuthTokenConfig
    {
        Secret = "super-secret-key-that-is-at-least-32-chars-long!",
        Issuer = "hoppyfy",
        Audience = "hoppyfy",
        AccessTokenDurationInDays = 1,
        RefreshTokenDurationInDays = 7,
        MaxSimultaneousLoggedInDevices = 5
    };

    public static S3TestConfig S3 = new S3TestConfig()
    {
        AccessKey = "test-access-key",
        SecretKey = "test-secret-key",
        ServiceUrl = "http://localhost:9000",
        BucketName = "test-bucket",
        PublicUrl = "http://localhost:9000/test-bucket",
        KeySalt = "test-salt",
        UseHttp = true,
        HostPort = 9000,
        ContainerPort = 9000,
    };
}