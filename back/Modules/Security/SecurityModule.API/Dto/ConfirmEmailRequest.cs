namespace SecurityModule.API.Dto;

public record ConfirmEmailRequest(string UserId, string Token);