using VenueModule.Domain.ValueObjects;

namespace VenueModule.API.B2B.Deals.Dto;

public record CreatePromotionRequest(PromoType Type, string Body);
