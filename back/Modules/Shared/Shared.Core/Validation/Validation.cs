using System.ComponentModel.DataAnnotations;
using System.Net;
using SharedCore.Exceptions;

namespace SharedCore.Validation;

public static class Validation
{
    private static EmailAddressAttribute _emailAttribute = new EmailAddressAttribute();

    private static void ThrowValidationError(string fieldName, string validationError)
    {
        throw new HttpCodeException(validationError, HttpStatusCode.UnprocessableEntity, ErrorCode.ValidationError);
    }

    public static void ThrowIfInvalid(string fieldName, Func<bool> isValid, string validationError)
    {
        if (!isValid())
        {
            ThrowValidationError(fieldName, validationError);
        }
    }

    public static void ThrowIfNull<T>(string fieldName, T value)
    {
        ThrowIfInvalid(fieldName,
            () => value != null,
            $"'{fieldName}' is required");
    }

    public static void ThrowIfNullOrWhiteSpace(string fieldName, string? value)
    {
        ThrowIfInvalid(fieldName,
            () => !string.IsNullOrWhiteSpace(value),
            $"'{fieldName}' cannot be empty");
    }

    public static void ThrowIfLengthExceeded(string fieldName, string value, int limit)
    {
        ThrowIfInvalid(fieldName,
            () => string.IsNullOrWhiteSpace(value) || value.Length <= limit,
            $"{fieldName} must be less then {limit} symbols length");
    }

    public static void ThrowIfNegativeOrZero(string fieldName, long value)
    {
        ThrowIfInvalid(fieldName,
            () => value > 0,
            $"{fieldName} must be a positive number");
    }

    public static void ThrowIfInvalidEmail(string fieldName, string value)
    {
        ThrowIfInvalid(fieldName,
            () => _emailAttribute.IsValid(value),
            $"'{fieldName}' must be a valid email address");
    }

    public static void ThrowIfInvalidUrl(string fieldName, string value)
    {
        ThrowIfInvalid(fieldName,
            () => IsValidUrl(value),
            $"'{fieldName}' must be a valid URL");
        return;

        bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
                   (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }

    public static void ThrowIfNotInRange(string fieldName, int value, int min, int max)
    {
        ThrowIfInvalid(fieldName,
            () => value >= min && value <= max,
            $"'{fieldName}' must be in range [{min}-{max}]");
    }
}