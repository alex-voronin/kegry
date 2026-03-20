using GeoTimeZone;
using NetTopologySuite.Geometries;
using SharedCore.Interfaces;
using SharedCore.Validation;

namespace VenueModule.Domain.Aggregates;

public class Venue : ISoftDeletable<Guid>
{
    public required Guid Id { get; init; }
    public required Guid QrCode { get; set; }
    public required Guid CompanyId { get; init; }
    public Company Company { get; init; } = null!;

    public string? CoverImageS3Key
    {
        get;
        set => SetProperty(
            ref field,
            ValidateS3Key(nameof(CoverImageS3Key), value)
        );
    }

    public required Point Coordinates
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                TimeZoneId = TimeZoneLookup.GetTimeZone(value.Y, value.X).Result;
            }
        }
    }

    public string TimeZoneId { get; private set; } = null!;

    public required string Address
    {
        get;
        set => SetProperty(
            ref field,
            ValidateAddress(nameof(Address), value)
        );
    } = null!;

    public string? WebsiteUrl
    {
        get;
        set => SetProperty(
            ref field,
            ValidateUrl(nameof(WebsiteUrl), value)
        );
    }

    public string? GoogleMapsUrl
    {
        get;
        set => SetProperty(
            ref field,
            ValidateGoogleMapsUrl(nameof(GoogleMapsUrl), value)
        );
    }

    public string? InstagramUrl
    {
        get;
        set => SetProperty(
            ref field,
            ValidateInstagramUrl(nameof(InstagramUrl), value)
        );
    }

    public string? PhoneNumber
    {
        get;
        set => SetProperty(
            ref field,
            ValidatePhoneNumber(nameof(PhoneNumber), value)
        );
    }

    public string? WorkingHours
    {
        get;
        set => SetProperty(
            ref field,
            ValidateWorkingHours(nameof(WorkingHours), value)
        );
    }

    public ICollection<Promo> Promos { get; set; } = new List<Promo>();

    private readonly List<Discount> _discounts = [];
    public IReadOnlyCollection<Discount> Discounts => _discounts.AsReadOnly();

    public DateTimeOffset ChangedAt { get; private set; }

    private bool SetProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        ChangedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public bool IsVerified { get; set; }
    public bool IsDeleted { get; set; }

    #region ================================= Validation =================================

    private static string? ValidateS3Key(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        Validation.ThrowIfLengthExceeded(fieldName, value, ValidationParameters.S3KeyMaxLength);
        Validation.ThrowIfInvalid(fieldName, () => !value.StartsWith('/'), "S3 key shouldn't start with /");
        return value.Trim();
    }

    private static string ValidateAddress(string fieldName, string value)
    {
        Validation.ThrowIfNullOrWhiteSpace(fieldName, value);
        Validation.ThrowIfLengthExceeded(fieldName, value, ValidationParameters.AddressMaxLength);
        return value.Trim();
    }

    private static string? ValidateUrl(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        Validation.ThrowIfLengthExceeded(fieldName, value, ValidationParameters.UrlMaxLength);
        Validation.ThrowIfInvalidUrl(fieldName, value);
        return value.Trim();
    }

    private static string? ValidatePhoneNumber(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        Validation.ThrowIfLengthExceeded(fieldName, value, ValidationParameters.PhoneNumberMaxLength);
        return value.Trim();
    }

    private static string? ValidateWorkingHours(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        Validation.ThrowIfLengthExceeded(fieldName, value, ValidationParameters.WorkingHoursMaxLength);
        return value.Trim();
    }

    private static string? ValidateGoogleMapsUrl(string fieldName, string? value)
    {
        var url = ValidateUrl(fieldName, value);
        if (url == null)
        {
            return null;
        }

        Validation.ThrowIfInvalid(fieldName,
            () => IsValidDomain(url, ValidationParameters.ValidGoogleDomains),
            "Is not a valid Google Maps URL");
        return url;
    }

    private static string? ValidateInstagramUrl(string fieldName, string? value)
    {
        var url = ValidateUrl(fieldName, value);
        if (url == null)
        {
            return null;
        }

        Validation.ThrowIfInvalid(fieldName,
            () => IsValidDomain(url, ValidationParameters.ValidInstagramDomains),
            "Is not a valid Instagram URL");
        return url;
    }

    private static bool IsValidDomain(string url, string[] validDomains)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        var validDomain = validDomains
            .Any(d => host == d || host.EndsWith("." + d));

        return validDomain;
    }

    #endregion
}