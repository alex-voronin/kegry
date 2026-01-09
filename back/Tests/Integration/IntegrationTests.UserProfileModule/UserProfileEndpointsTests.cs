using IntegrationTests.Shared;
using Serilog;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;
using SharedCore.ValueObjects;
using Shouldly;
using UserProfileModule.API.Dto;

namespace IntegrationTests.UserProfileModule;

[Collection("IntegrationTests")]
public class UserProfileEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationWebApplicationFactory _factory;

    public UserProfileEndpointsTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.ResetDatabaseAsync();

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserCreatesProfile_ThenProfileCreated()
    {
        // 1. Sign in
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authResponse);

        // 2. Create profile
        var request = new CreateUserProfileRequest(
            FirstName: "John",
            LastName: "Doe",
            Bio: "My bio",
            AvatarImageUrl: "http://example.com/avatar.jpg",
            CoverImageUrl: "http://example.com/cover.jpg",
            Birthday: new DateOnly(1990, 1, 1),
            Gender: Gender.Male,
            HobbyIds:
            [
                // hobby ids can be found in the user profile module's initial migration
                Guid.Parse("a436df38-2318-7ace-fca5-78d3189dbd8f"),
                Guid.Parse("0dc68446-71dc-39bc-3a1b-f007f5452372")
            ]
        );

        var response = await _factory.ApiClient.PostAsync<CreateUserProfileRequest, ApiResult.EmptyResult>(
            "/api/v1/user-profile", request);

        if (!response.Success)
        {
            Log.Error(response.Errors.First().Error);
        }

        // 3. Check response
        response.Success.ShouldBeTrue();
        var getResponse = await _factory.ApiClient.GetAsync<ApiResult<GetUserProfileResponse>>("/api/v1/user-profile");
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
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authResponse);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Update profile
        var request = new UpdateUserProfileRequest(
            FirstName: "UpdatedName",
            LastName: "UpdatedLastName",
            Bio: "Updated bio",
            AvatarImageUrl: "http://example.com/updated.jpg",
            CoverImageUrl: "http://example.com/updated_cover.jpg",
            Birthday: new DateOnly(1991, 2, 2),
            Gender: Gender.Female,
            HobbyIds: [Guid.Parse("33b2bf1f-e351-0658-5139-5ab721f5e935")]
        );

        var response = await _factory.ApiClient.PutAsync<UpdateUserProfileRequest, ApiResult.EmptyResult>(
            "/api/v1/user-profile", request);

        if (!response.Success)
        {
            Log.Error(response.Errors.First().Error);
        }
        response.Success.ShouldBeTrue();

        // 3. Check response
        var getResponse = await _factory.ApiClient.GetAsync<ApiResult<GetUserProfileResponse>>("/api/v1/user-profile");
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
    public async Task GivenNoProfile_WhenUserGetsProfile_ThenReturnsNotFound()
    {
        // 1. Sign in
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authResponse);

        // 2. Get profile
        var response = await _factory.ApiClient.GetAsync<ApiResult<GetUserProfileResponse>>("/api/v1/user-profile");

        // 3. Check response
        response.Success.ShouldBeFalse();
        response.Errors.First().Code.ShouldBe(nameof(ErrorCode.NotFound));
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserGetsAvatarUploadUrl_ThenReturnsUrl()
    {
        // 1. Sign in
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await _factory.ApiClient.PostAsync<object?, ApiResult<GetPresignedUrlResponse>>(
            "/api/v1/user-profile/avatar/upload-url", null);

        // 3. Check response
        response.Success.ShouldBeTrue();
        response.Result.UploadUrl.ShouldNotBeNullOrEmpty();
        response.Result.UploadUrl.ShouldContain("test-bucket");
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserGetsCoverUploadUrl_ThenReturnsUrl()
    {
        // 1. Sign in
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await _factory.ApiClient.PostAsync<object?, ApiResult<GetPresignedUrlResponse>>(
            "/api/v1/user-profile/cover/upload-url", null);

        // 3. Check response
        response.Success.ShouldBeTrue();
        response.Result.UploadUrl.ShouldNotBeNullOrEmpty();
        response.Result.UploadUrl.ShouldContain("test-bucket");
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserUploadsImageToS3_ThenTheyCanDownloadIt()
    {
        // 1. Sign in
        var authResponse = await _factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await _factory.ApiClient.PostAsync<object?, ApiResult<GetPresignedUrlResponse>>(
            "/api/v1/user-profile/avatar/upload-url", null);

        // 3. Check response
        response.Success.ShouldBeTrue();
        var uploadUrl = response.Result.UploadUrl;
        uploadUrl.ShouldNotBeNullOrEmpty();
        var downloadUrl = response.Result.PublicDownloadUrl;
        downloadUrl.ShouldNotBeNullOrEmpty();

        // 4. Upload dummy image
        var imageToUpload = "dummy image content"u8.ToArray();
        var uploadResponse = await _factory.S3Client.UploadAsync(uploadUrl, imageToUpload, "image/jpeg");
        uploadResponse.EnsureSuccessStatusCode();

        // 5. Verify in Minio (S3)
        var downloadedImage = await _factory.S3Client.Download(downloadUrl);
        downloadedImage.ShouldBe(imageToUpload);
    }
}