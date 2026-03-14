namespace VenueModule.API.B2B.Dto;

public record PatchCompanyRequest(
    string? Name,
    string? Email,
    string? AvatarImageUrl);
