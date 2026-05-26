namespace VenueModule.API.B2B.Dashboard.Dto;

public record VenueVisitsDto(string VenueId, string Address, VisitsChartDataPointDto[] DataPoints);

public record VisitsChartResponseDto(VenueVisitsDto[] Venues);
