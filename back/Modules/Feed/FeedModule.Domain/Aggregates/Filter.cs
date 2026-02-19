using FeedModule.Domain.Exceptions;
using SharedCore.ValueObjects;

namespace FeedModule.Domain.Aggregates;

public class Filter
{
    public const int MinAge = 18;
    public const int MaxDistanceMeters = 200_000;
    public const int MaxHobbies = 10;

    private int _radiusMeters;
    private int? _ageFrom;
    private int? _ageTo;
    private Guid[] _hobbyIds = [];

    public Guid UserId { get; init; }

    public int RadiusMeters
    {
        get => _radiusMeters;
        set
        {
            if (value < 0 || value > MaxDistanceMeters)
                throw FilterExceptions.InvalidDistance(MaxDistanceMeters / 1000);
            _radiusMeters = value;
        }
    }

    public int? AgeFrom
    {
        get => _ageFrom;
        set
        {
            if (value.HasValue && value.Value < MinAge)
                throw FilterExceptions.AgeTooLow(MinAge);
            if (value.HasValue && _ageTo.HasValue && value.Value > _ageTo.Value)
                throw FilterExceptions.InvalidAgeRange();
            _ageFrom = value;
        }
    }

    public int? AgeTo
    {
        get => _ageTo;
        set
        {
            if (value.HasValue && value.Value < MinAge)
                throw FilterExceptions.AgeTooLow(MinAge);
            if (_ageFrom.HasValue && value.HasValue && _ageFrom.Value > value.Value)
                throw FilterExceptions.InvalidAgeRange();
            _ageTo = value;
        }
    }

    public Gender? Gender { get; set; }

    public Guid[] HobbyIds
    {
        get => _hobbyIds;
        set
        {
            if (value.Length > MaxHobbies)
                throw FilterExceptions.TooManyHobbies(MaxHobbies);
            _hobbyIds = value;
        }
    }

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
    }

    public static Filter CreateDefault(Guid userId) =>
        new(userId, radiusMeters: 30000, ageFrom: null, ageTo: null, gender: null, hobbyIds: []);
}