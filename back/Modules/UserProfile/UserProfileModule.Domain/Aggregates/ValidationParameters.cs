namespace UserProfileModule.Domain.Aggregates;

public static class ValidationParameters
{
    public const int FirstNameMaxLength = 100;
    public const int LastNameMaxLength = 100;
    public const int BioMaxLength = 200;
    public const int MinAge = 18;
    public const int MinHobbiesCount = 1;
    public const int MaxHobbiesCount = 10;
    public const int S3KeyMaxLength = 128;
}