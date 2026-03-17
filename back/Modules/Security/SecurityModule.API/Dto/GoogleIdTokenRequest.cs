using SecurityModule.Domain.ValueObjects;

namespace SecurityModule.API.Dto;

public record GoogleIdTokenRequest(string IdToken, UserType UserType);
