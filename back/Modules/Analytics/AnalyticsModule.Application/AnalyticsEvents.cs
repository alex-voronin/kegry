namespace AnalyticsModule.Application;

public static class AnalyticsEvents
{
    public static class Account
    {
        public const string Created = "account_created_v1";
        public const string EmailConfirmationRequests = "email_confirmation_requested_v1";
        public const string Deleted = "account_deleted_v1";
        public const string PasswordResetRequested = "password_reset_requested_v1";
    }

    public static class Chat
    {
        public const string MessageSent = "message_sent_v1";
    }

    public static class Feed
    {
        public const string UserInvited = "user_invited_v1";
        public const string MeetingArranged = "meeting_arranged_v1";
        public const string MeetingEnded = "meeting_ended_v1";
    }

    public static class UserProfile
    {
        public const string Created = "user_profile_created_v1";
        public const string Updated = "user_profile_updated_v1";
    }

    public static class Location
    {
        public const string UsersMet = "users_met_by_location_v1";
    }

    public static class Engagement
    {
        public const string NpcInvited = "npc_invited_v1";
    }

    public static class Keys
    {
        public const string Email = "email";
        public const string OtherUserId = "other_user_id";
        public const string ParticipantIds = "participant_ids";
        public const string NpcId = "npc_id";
        public const string ExpiresAt = "expires_at";
        public const string Environment = "environment";
    }
}