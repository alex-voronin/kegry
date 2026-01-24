namespace NotificationsModule.API.Dto;

public record GetNotificationSettingsResponse(bool PushesEnabled, bool EmailsEnabled);
