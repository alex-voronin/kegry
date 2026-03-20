using NetTopologySuite.Geometries;
using SharedCore.Exceptions;
using Shouldly;
using VenueModule.Domain.Aggregates;

namespace Tests.VenueModule.Domain;

public class VenueTests
{
    [Fact]
    public void Venue_GivenValidData_WhenInitialized_ThenReturnsValidVenue()
    {
        var id = Guid.NewGuid();
        var venue = CreateValidVenue(id);

        venue.Id.ShouldBe(id);
        venue.Address.ShouldBe("123 Test St");
        venue.CoverImageS3Key.ShouldBe("venues/cover.jpg");
        venue.WebsiteUrl.ShouldBe("https://example.com");
        venue.PhoneNumber.ShouldBe("+1234567890");
    }

    [Fact]
    public void Venue_WhenValidValues_ThenVenueIsUpdated()
    {
        var venue = CreateValidVenue();
        var newAddress = "456 New St";
        var newWebsite = "https://new.com";
        var newPhone = "+0987654321";

        venue.Address = newAddress;
        venue.WebsiteUrl = newWebsite;
        venue.PhoneNumber = newPhone;

        venue.Address.ShouldBe(newAddress);
        venue.WebsiteUrl.ShouldBe(newWebsite);
        venue.PhoneNumber.ShouldBe(newPhone);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Address_WhenNullOrWhiteSpace_ThenThrows(string? invalidAddress)
    {
        var venue = CreateValidVenue();

        Should.Throw<HttpCodeException>(() => venue.Address = invalidAddress!)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Address_WhenLengthExceeded_ThenThrows()
    {
        var venue = CreateValidVenue();
        var longAddress = new string('a', ValidationParameters.AddressMaxLength + 1);

        Should.Throw<HttpCodeException>(() => venue.Address = longAddress)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CoverImageS3Key_WhenLengthExceeded_ThenThrows()
    {
        var venue = CreateValidVenue();
        var longKey = new string('a', ValidationParameters.S3KeyMaxLength + 1);

        Should.Throw<HttpCodeException>(() => venue.CoverImageS3Key = longKey)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void CoverImageS3Key_WhenStartsWithSlash_ThenThrows()
    {
        var venue = CreateValidVenue();
        var invalidKey = "/invalid/path.jpg";

        Should.Throw<HttpCodeException>(() => venue.CoverImageS3Key = invalidKey)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("http://")]
    [InlineData("https://")]
    public void WebsiteUrl_WhenInvalidFormat_ThenThrows(string invalidUrl)
    {
        var venue = CreateValidVenue();

        Should.Throw<HttpCodeException>(() => venue.WebsiteUrl = invalidUrl)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void WebsiteUrl_WhenLengthExceeded_ThenThrows()
    {
        var venue = CreateValidVenue();
        var longUrl = "https://" + new string('a', ValidationParameters.UrlMaxLength) + ".com";

        Should.Throw<HttpCodeException>(() => venue.WebsiteUrl = longUrl)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void PhoneNumber_WhenLengthExceeded_ThenThrows()
    {
        var venue = CreateValidVenue();
        var longPhone = new string('1', ValidationParameters.PhoneNumberMaxLength + 1);

        Should.Throw<HttpCodeException>(() => venue.PhoneNumber = longPhone)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void WorkingHours_WhenLengthExceeded_ThenThrows()
    {
        var venue = CreateValidVenue();
        var longHours = new string('a', ValidationParameters.WorkingHoursMaxLength + 1);

        Should.Throw<HttpCodeException>(() => venue.WorkingHours = longHours)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("https://www.instagram.com/acc")]
    [InlineData(null)]
    public void InstagramUrl_WhenValid_ThenSet(string? url)
    {
        var venue = CreateValidVenue();
        venue.InstagramUrl = url;
        venue.InstagramUrl.ShouldBe(url);
    }

    [Fact]
    public void InstagramUrl_WhenInvalid_ThenThrows()
    {
        var venue = CreateValidVenue();
        Should.Throw<HttpCodeException>(() => venue.InstagramUrl = "https://tiktok.com/@acc")
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("https://www.google.com/some-obj")]
    [InlineData(null)]
    public void GoogleMapsUrl_WhenValid_ThenSet(string? url)
    {
        var venue = CreateValidVenue();
        venue.GoogleMapsUrl = url;
        venue.GoogleMapsUrl.ShouldBe(url);
    }

    [Fact]
    public void GoogleMapsUrl_WhenInvalid_ThenThrows()
    {
        var venue = CreateValidVenue();
        Should.Throw<HttpCodeException>(() => venue.GoogleMapsUrl = "https://non-google.com/some-obj")
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    private static Venue CreateValidVenue(Guid? id = null)
    {
        return new Venue
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Coordinates = new Point(0, 0) { SRID = 4326 },
            Address = "123 Test St",
            CoverImageS3Key = "venues/cover.jpg",
            WebsiteUrl = "https://example.com",
            PhoneNumber = "+1234567890",
            WorkingHours = "Mon-Fri: 9am-5pm"
        };
    }
}
