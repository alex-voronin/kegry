namespace IntegrationTests.Shared;

public static class ApiRoutes
{
    public static class Auth
    {
        public const string Signup = "/api/v1/auth/signup";
        public const string ConfirmEmail = "/api/v1/auth/confirm-email";
        public const string Signin = "/api/v1/auth/signin";
        public const string RequestPasswordReset = "/api/v1/auth/request-password-reset";
        public const string ConfirmPasswordReset = "/api/v1/auth/confirm-password-reset";
        public const string RenewTokens = "/api/v1/auth/renew-tokens";
        public const string Account = "/api/v1/auth/account";
        public const string Logout = "/api/v1/auth/logout";
    }

    public static class Notifications
    {
        public const string GetNotifications = "/api/v1/notifications";
        public static string GetNotificationsPaginated(int offset, int limit) => $"/api/v1/notifications?offset={offset}&limit={limit}";
        public const string GetUnseenCount = "/api/v1/notifications/unseen/count";
        public const string UpdateDeviceToken = "/api/v1/notifications/device/token";
    }

    public static class Feed
    {
        public const string GetFeed = "/api/v1/feed";
        public const string GetStatus = $"/api/v1/feed/status";
        public static string RateUser(string userId) => $"/api/v1/feed/{userId}/rating";
        public static string InviteUser(string userId) => $"/api/v1/feed/{userId}/invite";
    }

    public static class UserProfile
    {
        public const string Profile = "/api/v1/user-profile";
        public const string AvatarUploadUrl = "/api/v1/user-profile/avatar/upload-url";
        public const string CoverUploadUrl = "/api/v1/user-profile/cover/upload-url";
    }

    public static class Location
    {
        public const string PostLocation = "/api/v1/location";
    }

    public static class Chat
    {
        public const string ChatRoot = "/api/v1/chat";
        public const string Message = "/api/v1/chat/message";
    }
}
