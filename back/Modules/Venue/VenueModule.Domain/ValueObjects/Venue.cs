using NetTopologySuite.Geometries;
using SharedCore.Validation;
using VenueModule.Domain.Aggregates;

namespace VenueModule.Domain.ValueObjects;

public class Venue
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public Company Company { get; init; } = null!;

    public string? CoverImageS3Key
    {
        get;
        set => SetProperty(
            ref field,
            ValidateS3Key(nameof(CoverImageS3Key), value)
        );
    }

    public required Point Coordinates { get; set; }

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
            ValidateUrl(nameof(GoogleMapsUrl), value)
        );
    }

    public string? InstagramUrl
    {
        get;
        set => SetProperty(
            ref field,
            ValidateUrl(nameof(InstagramUrl), value)
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
    public DateTimeOffset ChangedAt { get; private set; }

    private void SetProperty<T>(ref T field, T value)
    {
        field = value;
        ChangedAt = DateTimeOffset.UtcNow;
    }

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

    #endregion
}