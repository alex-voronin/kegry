namespace ChatModule.Domain.Aggregates;

public static class ValidationParameters
{
    public const int MaxUsersPerChat = 30;
    public const int MaxMessagesPerUser = 30;
    public const int MaxMessageLength = 2048;
}