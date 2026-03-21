using VenueModule.Domain.ValueObjects;

namespace VenueModule.API.B2C.Dto;

public record PromoDto(PromoType Type, string Body);
