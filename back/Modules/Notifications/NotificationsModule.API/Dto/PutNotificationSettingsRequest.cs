namespace NotificationsModule.API.Dto;

public record PutNotificationSettingsRequest(bool PushesEnabled, bool EmailsEnabled);
