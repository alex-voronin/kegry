namespace NotificationsModule.API.Dto;

public record NotificationDto(
    Guid Id,
    Guid? FromUserId,
    string? AvatarImageUrl,
    string PrimaryText,
    string SecondaryText,
    DateTimeOffset CreatedAt);