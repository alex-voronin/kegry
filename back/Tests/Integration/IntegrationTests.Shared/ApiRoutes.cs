namespace IntegrationTests.Shared;

public static class ApiRoutes
{
    public static class Auth
    {
        public const string Signup = "/api/v1/auth/signup";
        public const string ConfirmEmail = "/api/v1/auth/confirm-email";
        public const string ResendConfirmationEmail = "/api/v1/auth/resend-confirmation-email";
        public const string Signin = "/api/v1/auth/signin";
        public const string RequestPasswordReset = "/api/v1/auth/request-password-reset";
        public const string ConfirmPasswordReset = "/api/v1/auth/confirm-password-reset";
        public const string RenewTokens = "/api/v1/auth/renew-tokens";
        public const string Account = "/api/v1/auth/account";
        public const string Logout = "/api/v1/auth/logout";
        public const string Google = "/api/v1/auth/google";
    }

    public static class Notifications
    {
        public const string GetNotifications = "/api/v1/notifications";

        public static string GetNotificationsPaginated(int offset, int limit) =>
            $"/api/v1/notifications?offset={offset}&limit={limit}";

        public const string GetUnseenCount = "/api/v1/notifications/unseen/count";
        public const string UpdateDeviceToken = "/api/v1/notifications/device/token";

        public static string DeleteDeviceToken(string deviceId) =>
            $"/api/v1/notifications/device/token?deviceId={deviceId}";

        public const string Settings = "/api/v1/notifications/settings";
        public static string DeleteNotification(Guid id) => $"/api/v1/notifications/{id}";
    }

    public static class Feed
    {
        public const string GetFeed = "/api/v1/feed";
        public const string GetStatus = "/api/v1/feed/status";
        public static string RateUser(string userId) => $"/api/v1/feed/{userId}/rating";
        public static string InviteUser(string userId) => $"/api/v1/feed/{userId}/invite";
        public static string AcceptInvite(string userId) => $"/api/v1/feed/{userId}/accept-invite";
        public static string GetInviteExpiration(string userId) => $"/api/v1/feed/{userId}/invite-expiration";
        public static string BlockUser(string userId) => $"/api/v1/feed/{userId}/block";
        public static string SkipUser(string userId) => $"/api/v1/feed/{userId}/skip";
        public static string IgnoreUser(string userId) => $"/api/v1/feed/{userId}/ignore";
        public const string Filter = "/api/v1/feed/filter";
        public const string GetBlockedUsers = "/api/v1/feed/blocked-users";
        public const string GetFriends = "/api/v1/feed/friends";
        public static string InviteFriend(string userId) => $"/api/v1/feed/friends/{userId}/invite";
        public static string RemoveFriend(string userId) => $"/api/v1/feed/{userId}/remove-friend";
        public const string DisplaySettings = "/api/v1/feed/settings";
    }

    public static class UserProfile
    {
        public const string Profile = "/api/v1/user-profile";
        public const string Privacy = "/api/v1/user-profile/privacy";
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

    public static class Npc
    {
        public const string Root = "/api/v1/npc";
        public const string MetNpcs = "/api/v1/npc/met";
        public static string Invite(string id) => $"/api/v1/npc/{id}/invite";
    }

    public static class Venue
    {
        public const string Root = "/api/v1/venues";
        public const string Map = "/api/v1/venues/map";
        public const string Tags = "/api/v1/tags";
        public static string Single(Guid id) => $"/api/v1/venue/{id}";
        public static string Discounts(Guid id) => $"/api/v1/venue/{id}/discounts";
        public static string QrCheckIn(Guid qrCode) => $"/api/v1/venue/{qrCode}/qr-check-in";
        public static string CheckInSetting(Guid qrCode) => $"/api/v1/venue/{qrCode}/qr-check-in/settings";
    }

    public static class Deals
    {
        public const string Discounts = "/api/v1/discounts";
        public const string Promotions = "/api/v1/promotions";
    }

    public static class B2B
    {
        public static class Company
        {
            public const string Root = "/api/v1/b2b/companies";
            public static string Single(Guid id) => $"/api/v1/b2b/company/{id}";
            public static string Venues(Guid companyId) => $"/api/v1/b2b/company/{companyId}/venue";
            public static string LoyaltyLevels(Guid companyId) => $"/api/v1/b2b/company/{companyId}/loyalty-levels";
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static class Venue
        {
            public const string Root = "/api/v1/b2b/venues";
            public static string Single(Guid id) => $"/api/v1/b2b/venue/{id}";
            public static string Promotions(Guid venueId) => $"/api/v1/b2b/venue/{venueId}/promotions";
            public static string Promotion(Guid venueId, Guid id) => $"/api/v1/b2b/venue/{venueId}/promotions/{id}";
            public static string Discounts(Guid venueId) => $"/api/v1/b2b/venue/{venueId}/discounts";
            public static string Discount(Guid venueId, Guid id) => $"/api/v1/b2b/venue/{venueId}/discounts/{id}";
        }
    }
}