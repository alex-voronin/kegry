using SharedCore.Interfaces;
using SharedCore.Validation;

namespace VenueModule.Domain.Aggregates;

public class Company : ISoftDeletable<Guid>
{
    public Guid Id { get; init; }
    public Guid OwnerId { get; init; }

    public required string Name
    {
        get;
        set => SetProperty(
            ref field,
            ValidateName(nameof(Name), value)
        );
    } = null!;

    public string? AvatarImageS3Key
    {
        get;
        set => SetProperty(
            ref field,
            ValidateS3Key(nameof(AvatarImageS3Key), value)
        );
    }

    public string? Email
    {
        get;
        set => SetProperty(
            ref field,
            ValidateEmail(nameof(Email), value)
        );
    }

    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ChangedAt { get; private set; }
    private readonly List<LoyaltyLevel> _loyaltyLevels = [];
    public IReadOnlyCollection<LoyaltyLevel> LoyaltyLevels => _loyaltyLevels.AsReadOnly();
    public IReadOnlyCollection<Venue> Venues { get; init; } = new List<Venue>();
    public bool IsDeleted { get; set; }

    public void SetDefaultLoyaltyLevels()
    {
        if (_loyaltyLevels.Any())
        {
            throw new InvalidOperationException("Default loyalty levels are already set.");
        }

        _loyaltyLevels.AddRange([
            new LoyaltyLevel { Id = 0, CompanyId = Id, VisitsUpperBound = 3, IsActive = false},
            new LoyaltyLevel { Id = 1, CompanyId = Id, VisitsUpperBound = 10, IsActive = false},
            new LoyaltyLevel { Id = 2, CompanyId = Id, VisitsUpperBound = 15, IsActive = false},
            new LoyaltyLevel { Id = 3, CompanyId = Id, VisitsUpperBound = 20, IsActive = false}
        ]);
    }

    #region ================================= Validation =================================

    private void SetProperty<T>(ref T field, T value)
    {
        field = value;
        ChangedAt = DateTimeOffset.UtcNow;
    }

    private static string ValidateName(string fieldName, string value)
    {
        Validation.ThrowIfNullOrWhiteSpace(fieldName, value);
        Validation.ThrowIfLengthExceeded(fieldName, value, ValidationParameters.CompanyNameMaxLength);
        return value.Trim();
    }

    private static string ValidateS3Key(string fieldName, string? value)
    {
        Validation.ThrowIfNullOrWhiteSpace(fieldName, value);
        Validation.ThrowIfLengthExceeded(fieldName, value!, ValidationParameters.S3KeyMaxLength);
        Validation.ThrowIfInvalid(fieldName, () => !value!.StartsWith('/'), "S3 key shouldn't start with /");
        return value!.Trim();
    }

    private static string? ValidateEmail(string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        Validation.ThrowIfInvalidEmail(fieldName, value);
        return value.Trim().ToLowerInvariant();
    }

    #endregion
}