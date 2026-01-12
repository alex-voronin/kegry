namespace SecurityModule.API.Dto;

public record ConfirmPasswordResetRequest(string UserId, string Token, string NewPassword);
