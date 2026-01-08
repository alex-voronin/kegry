namespace IntegrationTests.Shared;

public static class TestConfig
{
    public static class AuthToken
    {
        public const string Secret = "super-secret-key-that-is-at-least-32-chars-long!";
        public const string Issuer = "hoppyfy.me";
        public const string Audience = "hoppyfy.me";
        public const string AccessTokenDurationInDays = "1";
        public const string RefreshTokenDurationInDays = "7";
        public const string MaxSimultaneousLoggedInDevices = "5";
    }

    public static class Db
    {
        internal const string User = "postgres";
        internal const string Password = "admin";
        internal const string DbName = "hoppyfydb";
        internal const int HostPort = 54321;
        internal const int ContainerPort = 5432;
    }
}