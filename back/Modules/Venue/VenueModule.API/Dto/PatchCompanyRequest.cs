namespace VenueModule.API.Dto;

public record PatchCompanyRequest(
    string? Name,
    string? Email,
    string? AvatarImageUrl);
