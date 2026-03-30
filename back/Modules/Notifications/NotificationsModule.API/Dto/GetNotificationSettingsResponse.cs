namespace NotificationsModule.API.Dto;

public record GetNotificationSettingsResponse(string Locale, bool PushesEnabled, bool EmailsEnabled);
