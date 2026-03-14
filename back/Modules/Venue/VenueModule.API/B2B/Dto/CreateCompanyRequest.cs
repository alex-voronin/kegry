namespace VenueModule.API.B2B.Dto;

public record CreateCompanyRequest(
    string Name,
    string? Email,
    string AvatarImageUrl);
