namespace NotificationModule.Domain.Aggregates;

public class Device
{
    public required string Id { get; init; }

    private string _fcmToken = null!;
    public required string FcmToken
    {
        get => field;
        init => field = value;
    }

    public required Guid UserId { get; set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Device()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateFcmToken(string fcmToken)
    {
        _fcmToken = fcmToken;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}