using SecurityModule.Application.Configs;
using UserProfileModule.Infrastructure.Storage.Configs;

namespace IntegrationTests.Shared;

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
        Issuer = "hoppyfy.me",
        Audience = "hoppyfy.me",
        AccessTokenDurationInDays = 1,
        RefreshTokenDurationInDays = 7,
        MaxSimultaneousLoggedInDevices = 5
    };

    public static S3Config S3 = new S3Config
    {
        AccessKey = "test-access-key",
        SecretKey = "test-secret-key",
        ServiceUrl = "https://test.r2.cloudflarestorage.com",
        BucketName = "test-bucket",
        PublicUrl = "https://test.r2.cloudflarestorage.com",
        KeySalt = "test-salt",
        UseHttp = true
    };
}