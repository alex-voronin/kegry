using VenueModule.API.B2B.Discount.Dto;

namespace VenueModule.API.B2B.Companies.Dto;

public record CompanyDto(
    string Id,
    string Name,
    string? Email,
    string? AvatarImageUrl,
    LoyaltyLevelDto[] LoyaltyLevels);