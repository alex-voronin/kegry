using System.Net;
using SharedCore.Exceptions;

namespace FeedModule.Domain.Exceptions;

public static class FilterExceptions
{
    public static HttpCodeException InvalidDistance(int maxKm)
    {
        return new HttpCodeException($"Distance must be between 0 and {maxKm} km",
            HttpStatusCode.BadRequest,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException AgeTooLow(int minAge)
    {
        return new HttpCodeException($"Age cannot be less than {minAge}",
            HttpStatusCode.BadRequest,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException InvalidAgeRange()
    {
        return new HttpCodeException("'Age from' cannot be greater than 'Age to'",
            HttpStatusCode.BadRequest,
            ErrorCode.ValidationError);
    }

    public static HttpCodeException TooManyHobbies(int maxHobbies)
    {
        return new HttpCodeException($"Cannot select more than {maxHobbies} hobbies",
            HttpStatusCode.BadRequest,
            ErrorCode.ValidationError);
    }
}