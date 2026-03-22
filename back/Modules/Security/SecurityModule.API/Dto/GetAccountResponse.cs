using SecurityModule.Domain.ValueObjects;

namespace SecurityModule.API.Dto;

public record GetAccountResponse(Guid Id, UserType Type, string Email);