using SharedCore.Exceptions;
using Shouldly;
using VenueModule.Domain.Aggregates;

namespace Tests.VenueModule.Domain;

public class CompanyTests
{
    [Fact]
    public void Company_GivenValidData_WhenInitialized_ThenReturnsValidCompany()
    {
        var id = Guid.NewGuid();
        var company = CreateValidCompany(id);

        company.Id.ShouldBe(id);
        company.Name.ShouldBe("Test Company");
        company.AvatarImageS3Key.ShouldBe("avatars/company.jpg");
        company.Email.ShouldBe("test@company.com");
    }

    [Fact]
    public void Company_WhenValidValues_ThenCompanyIsUpdated()
    {
        var company = CreateValidCompany();
        var newName = "New Company Name";
        var newAvatar = "avatars/new-company.jpg";
        var newEmail = "new@company.com";

        company.Name = newName;
        company.AvatarImageS3Key = newAvatar;
        company.Email = newEmail;

        company.Name.ShouldBe(newName);
        company.AvatarImageS3Key.ShouldBe(newAvatar);
        company.Email.ShouldBe(newEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Name_WhenNullOrWhiteSpace_ThenThrows(string? invalidName)
    {
        var company = CreateValidCompany();

        Should.Throw<HttpCodeException>(() => company.Name = invalidName!)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Name_WhenLengthExceeded_ThenThrows()
    {
        var company = CreateValidCompany();
        var longName = new string('a', ValidationParameters.CompanyNameMaxLength + 1);

        Should.Throw<HttpCodeException>(() => company.Name = longName)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void AvatarImageS3Key_WhenNullOrWhiteSpace_ThenThrows(string? invalidKey)
    {
        var company = CreateValidCompany();

        Should.Throw<HttpCodeException>(() => company.AvatarImageS3Key = invalidKey!)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void AvatarImageS3Key_WhenLengthExceeded_ThenThrows()
    {
        var company = CreateValidCompany();
        var longKey = new string('a', ValidationParameters.S3KeyMaxLength + 1);

        Should.Throw<HttpCodeException>(() => company.AvatarImageS3Key = longKey)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void AvatarImageS3Key_WhenStartsWithSlash_ThenThrows()
    {
        var company = CreateValidCompany();
        var invalidKey = "/invalid/path.jpg";

        Should.Throw<HttpCodeException>(() => company.AvatarImageS3Key = invalidKey)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@domain.com")]
    public void Email_WhenInvalidFormat_ThenThrows(string invalidEmail)
    {
        var company = CreateValidCompany();

        Should.Throw<HttpCodeException>(() => company.Email = invalidEmail)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    private static Company CreateValidCompany(Guid? id = null)
    {
        return new Company
        {
            Id = id ?? Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Test Company",
            AvatarImageS3Key = "avatars/company.jpg",
            Email = "test@company.com"
        };
    }
}
