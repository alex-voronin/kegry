namespace NotificationsModule.API.Dto;

public record NotificationDto(
    Guid Id,
    Guid? FromUserId,
    string? AvatarImageUrl,
    string Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);