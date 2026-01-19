namespace SecurityModule.API.Dto;

public record ConfirmPasswordResetRequest(string EncodedEmail, string EncodedToken, string NewPassword);
