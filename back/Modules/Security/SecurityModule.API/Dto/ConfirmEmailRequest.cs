namespace SecurityModule.API.Dto;

public class ConfirmEmailRequest
{
    public required string UserId { get; init; }
    public required string Token { get; init; }
}