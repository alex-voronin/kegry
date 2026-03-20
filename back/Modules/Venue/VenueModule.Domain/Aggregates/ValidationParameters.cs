namespace VenueModule.Domain.Aggregates;

public static class ValidationParameters
{
    public const int CompanyNameMaxLength = 200;
    public const int S3KeyMaxLength = 128;
    public const int AddressMaxLength = 500;
    public const int UrlMaxLength = 500;
    public const int PhoneNumberMaxLength = 20;
    public const int WorkingHoursMaxLength = 100;

    public static readonly string[] ValidGoogleDomains =
    [
        "google.com",
        "www.google.com",
        "maps.google.com",
        "maps.app.goo.gl",
        "goo.gl"
    ];

    public static readonly string[] ValidInstagramDomains =
    [
        "instagram.com",
        "www.instagram.com",
        "m.instagram.com"
    ];
}