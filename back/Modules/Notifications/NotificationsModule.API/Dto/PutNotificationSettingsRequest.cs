namespace NotificationsModule.API.Dto;

public record PutNotificationSettingsRequest(string Locale, bool PushesEnabled, bool EmailsEnabled);
