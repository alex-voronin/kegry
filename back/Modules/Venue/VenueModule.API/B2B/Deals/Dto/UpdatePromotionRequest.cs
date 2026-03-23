using VenueModule.Domain.ValueObjects;

namespace VenueModule.API.B2B.Deals.Dto;

public record UpdatePromotionRequest(PromoType Type, string Body);
