using IntegrationTests.Shared;
using Serilog;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;
using SharedCore.Extensions;
using SharedCore.ValueObjects;
using Shouldly;
using UserProfileModule.API.Dto;

namespace IntegrationTests.UserProfileModule;

[Collection("IntegrationTests")]
public class UserProfileEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public UserProfileEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserCreatesProfile_ThenProfileCreated()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Create profile
        var request = new CreateUserProfileRequest(
            FirstName: "John",
            LastName: "Doe",
            Bio: "My bio",
            AvatarImageUrl: TestConfig.S3.PublicUrl + "/avatar.jpg",
            CoverImageUrl: TestConfig.S3.PublicUrl + "/cover.jpg",
            Birthday: new DateOnly(1990, 1, 1),
            Gender: Gender.Male,
            HobbyIds:
            [
                "camping".ToDeterministicGuid(),
                "boxing".ToDeterministicGuid()
            ]
        );

        var response = await Factory.ApiClient.PostAsync<CreateUserProfileRequest, ApiResult.EmptyResult>(
            ApiRoutes.UserProfile.Profile, request);

        if (!response.Success)
        {
            Log.Error(response.Errors.First().Error);
        }

        // 3. Check response
        response.Success.ShouldBeTrue();
        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);
        getResponse.Success.ShouldBeTrue();
        getResponse.Result.UserProfile.ShouldSatisfyAllConditions(
             p => p.FirstName.ShouldBe(request.FirstName),
             p => p.LastName.ShouldBe(request.LastName),
             p => p.Bio.ShouldBe(request.Bio),
             p => p.AvatarImageUrl.ShouldBe(request.AvatarImageUrl),
             p => p.CoverImageUrl.ShouldBe(request.CoverImageUrl),
             p => p.Birthday.ShouldBe(request.Birthday),
             p => p.Gender.ShouldBe(request.Gender),
             p => p.Hobbies.Select(h => h.Id).ShouldBe(request.HobbyIds, ignoreOrder: true)
            );
    }

    [Fact]
    public async Task GivenExistingProfile_WhenUserUpdatesProfile_ThenProfileUpdated()
    {
        // 1. Sign in and create profile
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Update profile
        var request = new PatchUserProfileRequest(
            FirstName: "UpdatedName",
            LastName: "UpdatedLastName",
            Bio: "Updated bio",
            AvatarImageUrl: TestConfig.S3.PublicUrl + "/updated.jpg",
            CoverImageUrl: TestConfig.S3.PublicUrl + "/updated_cover.jpg",
            Birthday: new DateOnly(1991, 2, 2),
            Gender: Gender.Female,
            HobbyIds: ["tennis".ToDeterministicGuid()]
        );

        var response = await Factory.ApiClient.PatchAsync<PatchUserProfileRequest, ApiResult.EmptyResult>(
            ApiRoutes.UserProfile.Profile, request);

        if (!response.Success)
        {
            Log.Error(response.Errors.First().Error);
        }
        response.Success.ShouldBeTrue();

        // 3. Check response
        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);
        getResponse.Success.ShouldBeTrue();
        getResponse.Result.UserProfile.ShouldSatisfyAllConditions(
            p => p.FirstName.ShouldBe(request.FirstName),
            p => p.LastName.ShouldBe(request.LastName),
            p => p.Bio.ShouldBe(request.Bio),
            p => p.AvatarImageUrl.ShouldBe(request.AvatarImageUrl),
            p => p.CoverImageUrl.ShouldBe(request.CoverImageUrl),
            p => p.Birthday.ShouldBe(request.Birthday!.Value),
            p => p.Gender.ShouldBe(request.Gender!.Value),
            p => p.Hobbies.Select(h => h.Id).ShouldBe(request.HobbyIds, ignoreOrder: true)
        );
    }

    [Fact]
    public async Task GivenExistingProfile_WhenUserUpdatesAvatar_ThenOnlyAvatarUpdated()
    {
        // 1. Sign in and create profile
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get profile before update
        var getBeforeUpdateResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);

        // 3. Update profile
        var request = new PatchUserProfileRequest(
            FirstName: null,
            LastName: null,
            Bio: null,
            AvatarImageUrl: TestConfig.S3.PublicUrl + "/updated.jpg",
            CoverImageUrl: null,
            Birthday: null,
            Gender: null,
            HobbyIds: null
        );

        var response = await Factory.ApiClient.PatchAsync<PatchUserProfileRequest, ApiResult.EmptyResult>(
            ApiRoutes.UserProfile.Profile, request);

        if (!response.Success)
        {
            Log.Error(response.Errors.First().Error);
        }
        response.Success.ShouldBeTrue();

        // 3. Check response
        var getAfterUpdateResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);
        getAfterUpdateResponse.Success.ShouldBeTrue();
        getAfterUpdateResponse.Result.UserProfile.ShouldSatisfyAllConditions(
            p => p.FirstName.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.FirstName),
            p => p.LastName.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.LastName),
            p => p.Bio.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Bio),
            p => p.AvatarImageUrl.ShouldBe(request.AvatarImageUrl),
            p => p.CoverImageUrl.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.CoverImageUrl),
            p => p.Birthday.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Birthday),
            p => p.Gender.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Gender),
            p => p.Hobbies.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Hobbies, ignoreOrder: true)
        );
    }

    [Fact]
    public async Task GivenExistingProfile_WhenUserUpdatesCoverImage_ThenOnlyCoverImageUpdated()
    {
        // 1. Sign in and create profile
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get profile before update
        var getBeforeUpdateResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);

        // 3. Update profile
        var request = new PatchUserProfileRequest(
            FirstName: null,
            LastName: null,
            Bio: null,
            AvatarImageUrl: null,
            CoverImageUrl: TestConfig.S3.PublicUrl + "/updated.jpg",
            Birthday: null,
            Gender: null,
            HobbyIds: null
        );

        var response = await Factory.ApiClient.PatchAsync<PatchUserProfileRequest, ApiResult.EmptyResult>(
            ApiRoutes.UserProfile.Profile, request);

        if (!response.Success)
        {
            Log.Error(response.Errors.First().Error);
        }
        response.Success.ShouldBeTrue();

        // 3. Check response
        var getAfterUpdateResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);
        getAfterUpdateResponse.Success.ShouldBeTrue();
        getAfterUpdateResponse.Result.UserProfile.ShouldSatisfyAllConditions(
            p => p.FirstName.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.FirstName),
            p => p.LastName.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.LastName),
            p => p.Bio.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Bio),
            p => p.AvatarImageUrl.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.AvatarImageUrl),
            p => p.CoverImageUrl.ShouldBe(request.CoverImageUrl),
            p => p.Birthday.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Birthday),
            p => p.Gender.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Gender),
            p => p.Hobbies.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Hobbies, ignoreOrder: true)
        );
    }

    [Fact]
    public async Task GivenNoProfile_WhenUserGetsProfile_ThenReturnsNotFound()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get profile
        var response = await Factory.ApiClient.GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);

        // 3. Check response
        response.Success.ShouldBeFalse();
        response.Errors.First().Code.ShouldBe(nameof(ErrorCode.NotFound));
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserGetsAvatarUploadUrl_ThenReturnsUrl()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await Factory.ApiClient.PostAsync<object?, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.UserProfile.AvatarUploadUrl, null);

        // 3. Check response
        response.Success.ShouldBeTrue();
        response.Result.UploadUrl.ShouldNotBeNullOrEmpty();
        response.Result.UploadUrl.ShouldContain("test-bucket");
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserGetsCoverUploadUrl_ThenReturnsUrl()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await Factory.ApiClient.PostAsync<object?, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.UserProfile.CoverUploadUrl, null);

        // 3. Check response
        response.Success.ShouldBeTrue();
        response.Result.UploadUrl.ShouldNotBeNullOrEmpty();
        response.Result.UploadUrl.ShouldContain("test-bucket");
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserUploadsImageToS3_ThenTheyCanDownloadIt()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await Factory.ApiClient.PostAsync<object?, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.UserProfile.AvatarUploadUrl, null);

        // 3. Check response
        response.Success.ShouldBeTrue();
        var uploadUrl = response.Result.UploadUrl;
        uploadUrl.ShouldNotBeNullOrEmpty();
        var downloadUrl = response.Result.PublicDownloadUrl;
        downloadUrl.ShouldNotBeNullOrEmpty();

        // 4. Upload dummy image
        var imageToUpload = "dummy image content"u8.ToArray();
        var uploadResponse = await Factory.S3Client.UploadAsync(uploadUrl, imageToUpload, "image/jpeg");
        uploadResponse.EnsureSuccessStatusCode();

        // 5. Verify in Minio (S3)
        var downloadedImage = await Factory.S3Client.Download(downloadUrl);
        downloadedImage.ShouldBe(imageToUpload);
    }
}