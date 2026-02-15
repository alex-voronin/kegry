using NotificationModule.Domain.ValueObjects;

namespace NotificationsModule.API.Dto;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    Guid? FromUserId,
    string? AvatarImageUrl,
    string Text,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);