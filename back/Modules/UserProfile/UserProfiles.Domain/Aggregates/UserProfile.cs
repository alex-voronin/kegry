using SharedCore.Validation;
using SharedCore.ValueObjects;

namespace UserProfiles.Domain.Aggregates;

public class UserProfile
{
    public Guid Id { get; init; }

    public required string FirstName
    {
        get;
        set => field = ValidateRequiredString(nameof(FirstName), value, maxLength: 100);
    } = null!;

    public required string LastName
    {
        get;
        set => field = ValidateRequiredString(nameof(LastName), value, maxLength: 100);
    } = null!;

    public required string Bio
    {
        get;
        set => field = ValidateRequiredString(nameof(Bio), value, maxLength: 200);
    } = null!;

    public required string AvatarImageUrl
    {
        get;
        set => field = ValidateImageUrl(nameof(AvatarImageUrl), value);
    } = null!;

    public required string CoverImageUrl
    {
        get;
        set => field = ValidateImageUrl(nameof(CoverImageUrl), value);
    } = null!;

    public required DateOnly Birthday
    {
        get;
        set => field = ValidateBirthday(nameof(Birthday), value);
    }

    public required Gender Gender { get; set; }

    private List<Hobby> _hobbies = new();

    public Hobby[] Hobbies
    {
        get => _hobbies.ToArray();
        set => _hobbies = ValidateHobbies(nameof(Hobbies), value);
    }

    #region ================================= Validation =================================

    private static string ValidateRequiredString(string fieldName, string value, int maxLength)
    {
        Validation.ThrowIfNullOrWhiteSpace(fieldName, value);
        Validation.ThrowIfLengthExceeded(fieldName, value, maxLength);
        return value.Trim();
    }

    private static string ValidateImageUrl(string fieldName, string value)
    {
        Validation.ThrowIfNullOrWhiteSpace(fieldName, value);
        Validation.ThrowIfInvalidUrl(fieldName, value);
        return value.Trim();
    }

    private static DateOnly ValidateBirthday(string fieldName, DateOnly value)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var minAge = 18;
        Validation.ThrowIfInvalid(fieldName,
            () => value < today.AddYears(-minAge),
            $"A user must be older than {minAge} years old.");
        Validation.ThrowIfInvalid(fieldName,
            () => value.Year > 1900,
            "Invalid birth date");
        return value;
    }

    private static List<Hobby> ValidateHobbies(string fieldName, Hobby[] value)
    {
        Validation.ThrowIfInvalid(fieldName,
            isValid: () => value.Length <= 10,
            validationError: "Maximum number of Hobbies is 10");
        return value.ToList();
    }

    #endregion
}