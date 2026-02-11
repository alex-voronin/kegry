using FeedModule.Domain.Aggregates;
using SharedCore.Exceptions;
using SharedCore.ValueObjects;
using Shouldly;

namespace Tests.FeedModule.Domain;

public class FilterTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Constructor_WhenDataIsValid_ThenCreateFilter()
    {
        var filter = new Filter(
            _userId,
            radiusMeters: 30000,
            ageFrom: 20,
            ageTo: 30,
            gender: Gender.Male,
            hobbyIds: [Guid.NewGuid()]
        );

        filter.ShouldNotBeNull();
        filter.UserId.ShouldBe(_userId);
        filter.RadiusMeters.ShouldBe(30000);
        filter.AgeFrom.ShouldBe(20);
        filter.AgeTo.ShouldBe(30);
        filter.Gender.ShouldBe(Gender.Male);
        filter.HobbyIds.ShouldHaveSingleItem();
    }

    [Fact]
    public void CreateDefault_WhenCalled_ThenReturnsFilterWithDefaultValues()
    {
        var filter = Filter.CreateDefault(_userId);

        filter.ShouldNotBeNull();
        filter.UserId.ShouldBe(_userId);
        filter.RadiusMeters.ShouldBe(30000);
        filter.AgeFrom.ShouldBeNull();
        filter.AgeTo.ShouldBeNull();
        filter.Gender.ShouldBeNull();
        filter.HobbyIds.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_WhenDistanceIsNegative_ThenThrowException()
    {
        var exception = Should.Throw<HttpCodeException>(() => new Filter(
            _userId,
            radiusMeters: -1,
            ageFrom: null,
            ageTo: null,
            gender: null,
            hobbyIds: []
        ));

        exception.Message.ShouldContain("Distance must be between 0 and 200 km");
    }

    [Fact]
    public void Constructor_WhenDistanceIsTooLarge_ThenThrowException()
    {
        var exception = Should.Throw<HttpCodeException>(() => new Filter(
            _userId,
            radiusMeters: 200001,
            ageFrom: null,
            ageTo: null,
            gender: null,
            hobbyIds: []
        ));

        exception.Message.ShouldContain("Distance must be between 0 and 200 km");
    }

    [Fact]
    public void Constructor_WhenAgeFromIsTooLow_ThenThrowException()
    {
        var exception = Should.Throw<HttpCodeException>(() => new Filter(
            _userId,
            radiusMeters: 10000,
            ageFrom: 17,
            ageTo: null,
            gender: null,
            hobbyIds: []
        ));

        exception.Message.ShouldContain("Age cannot be less than 18");
    }

    [Fact]
    public void Constructor_WhenAgeToIsTooLow_ThenThrowException()
    {
        var exception = Should.Throw<HttpCodeException>(() => new Filter(
            _userId,
            radiusMeters: 10000,
            ageFrom: null,
            ageTo: 17,
            gender: null,
            hobbyIds: []
        ));

        exception.Message.ShouldContain("Age cannot be less than 18");
    }

    [Fact]
    public void Constructor_WhenAgeFromIsGreaterThanAgeTo_ThenThrowException()
    {
        var exception = Should.Throw<HttpCodeException>(() => new Filter(
            _userId,
            radiusMeters: 10000,
            ageFrom: 25,
            ageTo: 20,
            gender: null,
            hobbyIds: []
        ));

        exception.Message.ShouldContain("'Age from' cannot be greater than 'Age to'");
    }

    [Fact]
    public void Constructor_WhenTooManyHobbies_ThenThrowException()
    {
        var hobbyIds = Enumerable.Range(0, 11).Select(_ => Guid.NewGuid()).ToArray();

        var exception = Should.Throw<HttpCodeException>(() => new Filter(
            _userId,
            radiusMeters: 10000,
            ageFrom: null,
            ageTo: null,
            gender: null,
            hobbyIds: hobbyIds
        ));

        exception.Message.ShouldContain("Cannot select more than 10 hobbies");
    }
}
