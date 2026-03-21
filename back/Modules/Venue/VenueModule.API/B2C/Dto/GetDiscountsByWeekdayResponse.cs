namespace VenueModule.API.B2C.Dto;

public record CurrentAndNextLevelDiscountDto(int CurrentLevelDiscount, int? NextLevelDiscount);

public record GetDiscountsByWeekdayResponse
(
     CurrentAndNextLevelDiscountDto Sunday,
     CurrentAndNextLevelDiscountDto Monday,
     CurrentAndNextLevelDiscountDto Tuesday,
     CurrentAndNextLevelDiscountDto Wednesday,
     CurrentAndNextLevelDiscountDto Thursday,
     CurrentAndNextLevelDiscountDto Friday,
     CurrentAndNextLevelDiscountDto Saturday
);