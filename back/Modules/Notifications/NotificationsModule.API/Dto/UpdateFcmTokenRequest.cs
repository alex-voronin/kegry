namespace NotificationsModule.API.Dto;

internal record UpdateFcmTokenRequest(string DeviceId, string FcmToken);