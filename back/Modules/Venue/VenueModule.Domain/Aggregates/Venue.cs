using GeoTimeZone;
using NetTopologySuite.Geometries;
using SharedCore.Interfaces;
using SharedCore.Validation;

namespace VenueModule.Domain.Aggregates;

public class Venue : ISoftDeletable<Guid>, IAuditable
{
    public required Guid Id { get; init; }

    public string PinCode
    {
        get;
        set => field = ValidatePinCode(nameof(PinCode), value);
    } = "";

    public required Guid QrCode { get; set; }
    public required Guid CompanyId { get; init; }
    public Company Company { get; init; } = null!;

    public string? CoverImageS3Key
    {
        get;
        set
        {
            field = ValidateS3Key(nameof(CoverImageS3Key), value);
            // It's kind of a hack, since AvatarImageS3Key typically doesn't change even when we upload a new image,
            // but we should still update UpdatedAt to revalidate the Cloudflare cache.
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public required Point Coordinates
    {
        get;
        set
        {
            field = value;
            TimeZoneId = TimeZoneLookup.GetTimeZone(value.Y, value.X).Result;
        }
    }

    public string TimeZoneId { get; private set; } = null!;

    public required string Address
    {
        get;
        set => field = ValidateAddress(nameof(Address), value);
    } = null!;

    public string? WebsiteUrl
    {
        get;
        set => field = ValidateUrl(nameof(WebsiteUrl), value);
    }

    public string? GoogleMapsUrl
    {
        get;
        set => field = ValidateGoogleMapsUrl(nameof(GoogleMapsUrl), value);
    }

    public string? InstagramUrl
    {
        get;
        set => field = ValidateInstagramUrl(nameof(InstagramUrl), value);
    }

    public string? PhoneNumber
    {
        get;
        set => field = ValidatePhoneNumber(nameof(PhoneNumber), value);
    }

    public string? WorkingHours
    {
        get;
        set => field = ValidateWorkingHours(nameof(WorkingHours), value);
    }

    private readonly List<Promo> _promos = [];
    public IReadOnlyCollection<Promo> Promos => _promos;

    private readonly List<Discount> _discounts = [];
    public IReadOnlyCollection<Discount> Discounts => _discounts;

    public bool IsVerified { get; set; }
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public void AddPromo(Promo promo)
    {
        _promos.Add(promo);
        Validation.ThrowIfInvalid(
            nameof(Promos),
            () =>
                _promos.Count <= ValidationParameters.MaxPromos,
            $"Up to {ValidationParameters.MaxPromos} promos are allowed");
    }

    public void AddDiscount(Discount discount)
    {
        _discounts.Add(discount);
        Validation.ThrowIfInvalid(
            nameof(Discounts),
            () =>
                _discounts.Count <= ValidationParameters.MaxDiscounts,
            $"Up to {ValidationParameters.MaxDiscounts} discounts are allowed");
    }

    #region ================================= Validation =================================

    private static string ValidatePinCode(string fieldName, string value)
    {
        Validation.ThrowIfNull(fieldName, value);

        var trimmed = value!.Trim();
        Validation.ThrowIfInvalid(fieldName,
            () => trimmed.Length == 4 || trimmed.Length == 0,
            "Pin code must be 4 digits or empty string"
        );
        return trimmed;
    }

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