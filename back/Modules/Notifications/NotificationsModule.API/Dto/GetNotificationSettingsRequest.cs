namespace NotificationsModule.API.Dto;

public record GetNotificationSettingsRequest(bool PushesEnabled, bool EmailsEnabled);
