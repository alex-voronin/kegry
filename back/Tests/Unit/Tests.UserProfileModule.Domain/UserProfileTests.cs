using SharedCore.Exceptions;
using SharedCore.ValueObjects;
using Shouldly;
using UserProfileModule.Domain.Aggregates;
using Hobby = UserProfileModule.Domain.Aggregates.Hobby;

namespace Tests.UserProfileModule.Domain;

public class UserProfileTests
{
    [Fact]
    public void UserProfile_GivenValidData_WhenInitialized_ThenReturnValidProfile()
    {
        var id = Guid.CreateVersion7();
        var profile = CreateValidProfile(id);

        profile.Id.ShouldBe(id);
        profile.FirstName.ShouldBe("John");
        profile.LastName.ShouldBe("Doe");
        profile.Bio.ShouldBe("A brief bio about John.");
        profile.AvatarImageUrl.ShouldBe("https://example.com/avatar.jpg");
        profile.CoverImageUrl.ShouldBe("https://example.com/cover.jpg");
        profile.Birthday.ShouldBe(new DateOnly(1990, 1, 1));
        profile.Gender.ShouldBe(Gender.Male);
        profile.Hobbies.Count.ShouldBe(1);
    }
    
    [Fact]
    public void UserProfile_WhenValidValues_ThenProfileIsUpdated()
    {
        var firstName = "Jack";
        var lastName = "Smith";
        var bio = string.Empty;
        var avatarUrl = "https://example.com/new-avatar.jpg";
        var coverUrl = "https://example.com/new-cover.jpg";
        var birthday = new DateOnly(1995, 5, 5);
        var gender = Gender.Male;
        var hobbies = new[] { new Hobby { Id = Guid.NewGuid(), Name = "Hiking" } };
        
        var profile = CreateValidProfile();
        
        profile.FirstName = firstName;
        profile.LastName = lastName;
        profile.Bio = bio;
        profile.AvatarImageUrl = avatarUrl;
        profile.CoverImageUrl = coverUrl;
        profile.Birthday = birthday;
        profile.Gender = gender;
        profile.Hobbies = hobbies;
        
        profile.FirstName.ShouldBe(firstName);
        profile.LastName.ShouldBe(lastName);
        profile.Bio.ShouldBe(bio);
        profile.AvatarImageUrl.ShouldBe(avatarUrl);
        profile.CoverImageUrl.ShouldBe(coverUrl);
        profile.Birthday.ShouldBe(birthday);
        profile.Gender.ShouldBe(gender);
        profile.Hobbies.Count.ShouldBe(hobbies.Length);
        profile.Hobbies[0].Id.ShouldBe(hobbies[0].Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FirstName_WhenNullOrWhiteSpace_ThenThrows(string? invalidName)
    {
        var profile = CreateValidProfile();

        Should.Throw<HttpCodeException>(() => profile.FirstName = invalidName!)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void LastName_WhenNullOrWhiteSpace_ThenThrows(string? invalidName)
    {
        var profile = CreateValidProfile();

        Should.Throw<HttpCodeException>(() => profile.LastName = invalidName!)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void FirstName_WhenLengthExceeded_ThenThrows()
    {
        var profile = CreateValidProfile();
        var longName = new string('a', ValidationParameters.FirstNameMaxLength + 1);

        Should.Throw<HttpCodeException>(() => profile.FirstName = longName)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }
    
    [Fact]
    public void LastName_WhenLengthExceeded_ThenThrows()
    {
        var profile = CreateValidProfile();
        var longName = new string('a', ValidationParameters.LastNameMaxLength + 1);

        Should.Throw<HttpCodeException>(() => profile.LastName = longName)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }
    
    [Fact]
    public void Bio_WhenLengthExceeded_ThenThrows()
    {
        var profile = CreateValidProfile();
        var longName = new string('a', ValidationParameters.BioMaxLength + 1);

        Should.Throw<HttpCodeException>(() => profile.Bio = longName)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("ftp://example.com")]
    [InlineData(null)]
    [InlineData("")]
    public void AvatarImageUrl_WhenInvalidUrl_ThenThrows(string? invalidUrl)
    {
        var profile = CreateValidProfile();

        Should.Throw<HttpCodeException>(() => profile.AvatarImageUrl = invalidUrl!)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }
    
    [Theory]
    [InlineData("invalid-url")]
    [InlineData("ftp://example.com")]
    [InlineData(null)]
    [InlineData("")]
    public void CoverImageUrl_WhenInvalidUrl_ThenThrows(string? invalidUrl)
    {
        var profile = CreateValidProfile();

        Should.Throw<HttpCodeException>(() => profile.AvatarImageUrl = invalidUrl!)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Birthday_WhenUserIsTooYoung_ThenThrows()
    {
        var profile = CreateValidProfile();
        var tooYoungBirthday = DateOnly.FromDateTime(DateTime.Today)
            .AddYears(-ValidationParameters.MinAge)
            .AddDays(1);

        Should.Throw<HttpCodeException>(() => profile.Birthday = tooYoungBirthday)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    [Fact]
    public void Hobbies_WhenTooMany_ThenThrows()
    {
        var profile = CreateValidProfile();
        var tooManyHobbies = Enumerable.Range(0, ValidationParameters.MaxHobbiesCount + 1)
            .Select(_ => new Hobby { Id = Guid.NewGuid(), Name = "Hobby" })
            .ToArray();

        Should.Throw<HttpCodeException>(() => profile.Hobbies = tooManyHobbies)
            .ErrorCode.ShouldBe(ErrorCode.ValidationError);
    }

    private static UserProfile CreateValidProfile(Guid? id = null)
    {
        id ??= Guid.CreateVersion7();
        return new UserProfile
        {
            Id = id.Value,
            FirstName = "John",
            LastName = "Doe",
            Bio = "A brief bio about John.",
            AvatarImageUrl = "https://example.com/avatar.jpg",
            CoverImageUrl = "https://example.com/cover.jpg",
            Birthday = new DateOnly(1990, 1, 1),
            Gender = Gender.Male,
            Hobbies = new[] { new Hobby { Id = Guid.NewGuid(), Name = "Coding" } }
        };
    }
}