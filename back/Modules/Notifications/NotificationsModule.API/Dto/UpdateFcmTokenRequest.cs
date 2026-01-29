namespace NotificationsModule.API.Dto;

public record UpdateFcmTokenRequest(string DeviceId, string FcmToken);