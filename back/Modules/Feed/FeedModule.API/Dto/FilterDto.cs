using FeedModule.Domain.ValueObjects;
using SharedCore.ValueObjects;

namespace FeedModule.API.Dto;

public record FilterDto(
    int RadiusMeters,
    int? AgeFrom,
    int? AgeTo,
    Gender? Gender,
    Guid[] HobbyIds);

public record GetFilterResponse(FilterDto Filter);

public record UpdateFilterRequest(FilterDto Filter);
