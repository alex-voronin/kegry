namespace VenueModule.API.Dto;

public record CreateCompanyRequest(
    string Name,
    string? Email,
    string AvatarImageUrl);
