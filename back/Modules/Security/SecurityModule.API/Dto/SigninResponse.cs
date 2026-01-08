namespace SecurityModule.API.Dto;

public record SigninResponse(bool NeedSignup, AuthResponse? Auth);