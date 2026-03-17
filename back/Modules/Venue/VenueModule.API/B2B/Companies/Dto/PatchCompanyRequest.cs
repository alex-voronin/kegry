namespace VenueModule.API.B2B.Companies.Dto;

public record PatchCompanyRequest(
    string? Name,
    string? Email,
    string? AvatarImageUrl);
