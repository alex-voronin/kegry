using FeedModule.API.Dto;
using FeedModule.Domain.Aggregates;
using IntegrationTests.Shared;
using SharedCore.Api.Dto;
using SharedCore.Extensions;
using SharedCore.ValueObjects;
using Shouldly;

namespace IntegrationTests.FeedModule;

[Collection("IntegrationTests")]
public class FeedFiltersTests(IntegrationWebApplicationFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await factory.ResetDatabaseAsync();

    [Fact]
    public async Task GivenNoSavedFilter_WhenUserGetsFilter_ThenReturnsDefaultFilter()
    {
        // 1. Login user
        var auth = await factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        factory.ApiClient.SetAuth(auth);

        // 2. Get filter
        var response = await factory.ApiClient.GetAsync<ApiResult<GetFilterResponse>>(ApiRoutes.Feed.Filter);

        // 3. Check that default filter is returned
        var defaultFilter = Filter.CreateDefault(Guid.NewGuid());
        response.Success.ShouldBeTrue();
        response.Result.Filter.RadiusMeters.ShouldBe(defaultFilter.RadiusMeters);
        response.Result.Filter.AgeFrom.ShouldBe(defaultFilter.AgeFrom);
        response.Result.Filter.AgeTo.ShouldBe(defaultFilter.AgeTo);
        response.Result.Filter.Gender.ShouldBe(defaultFilter.Gender);
        response.Result.Filter.HobbyIds.ShouldBe(defaultFilter.HobbyIds);
    }

    [Fact]
    public async Task GivenUser_WhenUpdatesFilter_ThenReturnsUpdatedFilter()
    {
        // 1. Login user
        var auth = await factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        factory.ApiClient.SetAuth(auth);

        // 2. Update filter
        var newFilter = new FilterDto(
            RadiusMeters: 5000,
            AgeFrom: 20,
            AgeTo: 30,
            Gender: Gender.Female,
            HobbyIds: []);

        var updateResponse = await factory.ApiClient.PutAsync<UpdateFilterRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.Filter, new UpdateFilterRequest(newFilter));
        updateResponse.Success.ShouldBeTrue();

        // 3. Get filter again
        var getResponse = await factory.ApiClient.GetAsync<ApiResult<GetFilterResponse>>(ApiRoutes.Feed.Filter);

        // 4. Check new filter parameters
        getResponse.Success.ShouldBeTrue();
        getResponse.Result.Filter.RadiusMeters.ShouldBe(newFilter.RadiusMeters);
        getResponse.Result.Filter.AgeFrom.ShouldBe(newFilter.AgeFrom);
        getResponse.Result.Filter.AgeTo.ShouldBe(newFilter.AgeTo);
        getResponse.Result.Filter.Gender.ShouldBe(newFilter.Gender);
    }

    [Fact]
    public async Task GivenMultipleUsers_WhenFilterByGender_ThenReturnsOnlyFilteredGender()
    {
        // 1. Create main user (male)
        var authMain = await factory.AuthManager.CreateAndLoginUserAsync("main_male@example.com", "Password123!");
        factory.ApiClient.SetAuth(authMain);
        await factory.AuthManager.CreateProfile(firstName: "Main", lastName: "Male", gender: Gender.Male);
        await factory.AuthManager.SubmitLocation();

        // 2. Create male candidate (should be in feed)
        var authMaleCandidate =
            await factory.AuthManager.CreateAndLoginUserAsync("candidate_male@example.com", "Password123!");
        factory.ApiClient.SetAuth(authMaleCandidate);
        await factory.AuthManager.CreateProfile(firstName: "Male", lastName: "Candidate", gender: Gender.Male);
        await factory.AuthManager.SubmitLocation();

        // 3. Create female candidate (should NOT be in feed)
        var authFemaleCandidate =
            await factory.AuthManager.CreateAndLoginUserAsync("candidate_female@example.com", "Password123!");
        factory.ApiClient.SetAuth(authFemaleCandidate);
        await factory.AuthManager.CreateProfile(firstName: "Female", lastName: "Candidate", gender: Gender.Female);
        await factory.AuthManager.SubmitLocation();

        // 4. Update main user's filter to Male
        factory.ApiClient.SetAuth(authMain);
        var filter = new FilterDto(
            RadiusMeters: 30000,
            AgeFrom: null,
            AgeTo: null,
            Gender: Gender.Male,
            HobbyIds: []);
        await factory.ApiClient.PutAsync<UpdateFilterRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.Filter, new UpdateFilterRequest(filter));

        // 5. Get feed
        var feedResponse = await factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);

        // 6. Check that only males on the feed
        feedResponse.Result.Feed.Length.ShouldBe(1);
        feedResponse.Result.Feed[0].FirstName.ShouldBe("Male");
    }

    [Fact]
    public async Task GivenMultipleUsers_WhenFilterByRadius_ThenReturnsOnlyUsersWithinRadius()
    {
        // 1. Create main user
        var authMain = await factory.AuthManager.CreateAndLoginUserAsync("main_radius@example.com", "Password123!");
        factory.ApiClient.SetAuth(authMain);
        await factory.AuthManager.CreateProfile(firstName: "Main", lastName: "Static", gender: Gender.Male);
        await factory.AuthManager.SubmitLocation(52.2297, 21.0122);

        // 2. Create near candidate (approx 1km)
        var authNear = await factory.AuthManager.CreateAndLoginUserAsync("candidate_near@example.com", "Password123!");
        factory.ApiClient.SetAuth(authNear);
        await factory.AuthManager.CreateProfile(firstName: "Near", lastName: "Candidate", gender: Gender.Male);
        await factory.AuthManager.SubmitLocation(52.2297, 21.0122);

        // 3. Create far candidate (approx 50km)
        var authFar = await factory.AuthManager.CreateAndLoginUserAsync("candidate_far@example.com", "Password123!");
        factory.ApiClient.SetAuth(authFar);
        await factory.AuthManager.CreateProfile(firstName: "Far", lastName: "Candidate", gender: Gender.Male);
        await factory.AuthManager.SubmitLocation(52.6297, 21.4122);

        // 4. Update filter to 5km radius
        factory.ApiClient.SetAuth(authMain);
        var filter = new FilterDto(
            RadiusMeters: 5000,
            AgeFrom: null,
            AgeTo: null,
            Gender: null,
            HobbyIds: []);
        await factory.ApiClient.PutAsync<UpdateFilterRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.Filter, new UpdateFilterRequest(filter));

        // 5. Get feed
        var feedResponse = await factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);

        // 6. Check that only "near" candidate on the feed 
        feedResponse.Result.Feed.Length.ShouldBe(1);
        feedResponse.Result.Feed[0].FirstName.ShouldBe("Near");
    }

    [Fact]
    public async Task GivenMultipleUsers_WhenFilterByAge_ThenReturnsOnlyFilteredAgeRange()
    {
        // 1. Create main user
        var authMain = await factory.AuthManager.CreateAndLoginUserAsync("main_age@example.com", "Password123!");
        factory.ApiClient.SetAuth(authMain);
        await factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Create candidate in age range (e.g., 25 years old if today is 2026)
        var authInAge =
            await factory.AuthManager.CreateAndLoginUserAsync("candidate_in_age@example.com", "Password123!");
        factory.ApiClient.SetAuth(authInAge);
        await factory.AuthManager.CreateProfile(
                firstName: "MatchedAge",
                lastName: "In",
                gender: Gender.Male,
                birthday: new DateOnly(2000, 1, 1)
            );
        await factory.AuthManager.SubmitLocation();

        // 3. Create candidate out of age range (e.g., 45 years old)
        var authOutAge =
            await factory.AuthManager.CreateAndLoginUserAsync("candidate_out_age@example.com", "Password123!");
        factory.ApiClient.SetAuth(authOutAge);
        await factory.AuthManager.CreateProfile(
            firstName: "OutAge",
            lastName: "Out",
            gender: Gender.Male,
            birthday: new DateOnly(1980, 1, 1)
        );
        await factory.AuthManager.SubmitLocation();

        // 4. Update filter to 20-30 age range
        factory.ApiClient.SetAuth(authMain);
        var filter = new FilterDto(
            RadiusMeters: 30000,
            AgeFrom: 20,
            AgeTo: 30,
            Gender: null,
            HobbyIds: []);
        await factory.ApiClient.PutAsync<UpdateFilterRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.Filter, new UpdateFilterRequest(filter));

        // 5. Get feed
        var feedResponse = await factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);

        // 6. Check that only candidates with matched age on the feed
        feedResponse.Result.Feed.Length.ShouldBe(1);
        feedResponse.Result.Feed[0].FirstName.ShouldBe("MatchedAge");
    }

    [Fact]
    public async Task GivenMultipleUsers_WhenFilterByHobbies_ThenReturnsOnlyUsersWithMatchingHobbies()
    {
        // 1. Create main user
        var authMain = await factory.AuthManager.CreateAndLoginUserAsync("main_hobbies@example.com", "Password123!");
        factory.ApiClient.SetAuth(authMain);
        await factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var gymHobbyId = "gym".ToDeterministicGuid();
        var runningHobbyId = "running".ToDeterministicGuid();

        // 2. Create candidate with gym hobby (should be matched)
        var authMatchedHobby =
            await factory.AuthManager.CreateAndLoginUserAsync("candidate_matched_hobby@example.com", "Password123!");
        factory.ApiClient.SetAuth(authMatchedHobby);
        await factory.AuthManager.CreateProfile(
            firstName: "MatchedHobby",
            hobbyIds: [gymHobbyId]);
        await factory.AuthManager.SubmitLocation();

        // 3. Create candidate with running hobby (should NOT be matched)
        var authUnmatchedHobby =
            await factory.AuthManager.CreateAndLoginUserAsync("candidate_unmatched_hobby@example.com", "Password123!");
        factory.ApiClient.SetAuth(authUnmatchedHobby);
        await factory.AuthManager.CreateProfile(
            firstName: "UnmatchedHobby",
            hobbyIds: [runningHobbyId]);
        await factory.AuthManager.SubmitLocation();

        // 4. Update main user's filter to Gym hobby
        factory.ApiClient.SetAuth(authMain);
        var filter = new FilterDto(
            RadiusMeters: 30000,
            AgeFrom: null,
            AgeTo: null,
            Gender: null,
            HobbyIds: [gymHobbyId]);
        await factory.ApiClient.PutAsync<UpdateFilterRequest, ApiResult.EmptyResult>(
            ApiRoutes.Feed.Filter, new UpdateFilterRequest(filter));

        // 5. Get feed
        var feedResponse = await factory.ApiClient.GetAsync<ApiResult<GetFeedResponse>>(ApiRoutes.Feed.GetFeed);

        // 6. Check that only candidate with matched hobby on the feed
        feedResponse.Result.Feed.Length.ShouldBe(1);
        feedResponse.Result.Feed[0].FirstName.ShouldBe("MatchedHobby");
    }
}