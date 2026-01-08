namespace SecurityModule.API.Dto;

public record SigninResponse(bool IsSignupRequired, AuthResponse? Auth);