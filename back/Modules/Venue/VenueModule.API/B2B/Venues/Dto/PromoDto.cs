using VenueModule.Domain.ValueObjects;

namespace VenueModule.API.B2B.Venues.Dto;

public record PromoDto(string Id, PromoType Type, string Body);
