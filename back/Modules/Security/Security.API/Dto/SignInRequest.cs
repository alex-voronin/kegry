namespace Security.API.Dto;

public class SignInRequest
{
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
}