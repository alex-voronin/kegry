using FeedModule.Domain.Exceptions;
using FeedModule.Domain.ValueObjects;
using SharedCore.ValueObjects;

namespace FeedModule.Domain.Aggregates;

public record Filter
{
    public const int MinAge = 18;
    public const int MaxDistanceMeters = 200_000;
    public const int MaxHobbies = 10;

    public Guid UserId { get; init; }
    public int RadiusMeters { get; init; }
    public int? AgeFrom { get; init; }
    public int? AgeTo { get; init; }
    public Gender? Gender { get; init; }
    public Guid[] HobbyIds { get; init; }

    public Filter(
        Guid userId,
        int radiusMeters,
        int? ageFrom,
        int? ageTo,
        Gender? gender,
        Guid[] hobbyIds)
    {
        UserId = userId;
        RadiusMeters = radiusMeters;
        AgeFrom = ageFrom;
        AgeTo = ageTo;
        Gender = gender;
        HobbyIds = hobbyIds;

        Validate();
    }

    private void Validate()
    {
        if (RadiusMeters < 0 || RadiusMeters > MaxDistanceMeters)
            throw FilterExceptions.InvalidDistance(MaxDistanceMeters / 1000);

        if (AgeFrom.HasValue && AgeFrom.Value < MinAge)
            throw FilterExceptions.AgeTooLow(MinAge);

        if (AgeTo.HasValue && AgeTo.Value < MinAge)
            throw FilterExceptions.AgeTooLow(MinAge);

        if (AgeFrom.HasValue && AgeTo.HasValue && AgeFrom.Value > AgeTo.Value)
            throw FilterExceptions.InvalidAgeRange();

        if (HobbyIds.Length > MaxHobbies)
            throw FilterExceptions.TooManyHobbies(MaxHobbies);
    }

    public static Filter CreateDefault(Guid userId) =>
        new(userId, radiusMeters: 30000, ageFrom: null, ageTo: null, gender: null, hobbyIds: []);
}