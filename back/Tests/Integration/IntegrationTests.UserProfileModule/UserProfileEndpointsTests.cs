using IntegrationTests.Shared;
using IntegrationTests.Shared.Extensions;
using NotificationModule.Domain.ValueObjects;
using NotificationsModule.API.Dto;
using Serilog;
using SharedCore.Api.Dto;
using SharedCore.Exceptions;
using SharedCore.Extensions;
using SharedCore.ValueObjects;
using Shouldly;
using UserProfileModule.API;
using UserProfileModule.API.Dto;
using Wolverine.Tracking;

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
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync(
            email: "test@example.com", password: "Password123!", locale: SupportedLocales.Serbian);
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Create profile
        var request = new CreateUserProfileRequest(
            ReferrerId: null,
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
        response.Success.ShouldBeTrue();

        // 3. Check response
        await Factory.Host.ExecuteAndWaitAsync(async () =>
        {
            var getResponse = await Factory.ApiClient
                .GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);
            getResponse.Success.ShouldBeTrue();
            getResponse.Result.UserProfile.ShouldSatisfyAllConditions(
                p => p.Id.ShouldBe(authResponse.UserId),
                p => p.FirstName.ShouldBe(request.FirstName),
                p => p.LastName.ShouldBe(request.LastName),
                p => p.Bio.ShouldBe(request.Bio),
                p => p.AvatarImageUrl.ShouldBeNewerOrTheSameS3Url(request.AvatarImageUrl),
                p => p.CoverImageUrl.ShouldBeNewerOrTheSameS3Url(request.CoverImageUrl),
                p => p.Birthday.ShouldBe(request.Birthday),
                p => p.Gender.ShouldBe(request.Gender),
                p => p.Hobbies.Select(h => h.Id).ShouldBe(request.HobbyIds, ignoreOrder: true)
            );
        });

        // 4. Check that notification settings have appropriate local
        var notificationSettingsResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetNotificationSettingsResponse>>(ApiRoutes.Notifications.Settings);
        notificationSettingsResponse.Success.ShouldBeTrue();
        notificationSettingsResponse.Result.Locale.ShouldBe(SupportedLocales.Serbian);
        notificationSettingsResponse.Result.PushesEnabled.ShouldBeTrue();
        notificationSettingsResponse.Result.EmailsEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenExistingProfile_WhenUserUpdatesProfile_ThenProfileUpdated()
    {
        // 1. Sign in and create profile
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Update profile
        var firstRequest = new PatchUserProfileRequest(
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
            ApiRoutes.UserProfile.Profile, firstRequest);
        response.Success.ShouldBeTrue();

        // 3. Update profile again
        var secondRequest = new PatchUserProfileRequest(
            FirstName: "UpdatedName",
            LastName: "UpdatedLastName",
            Bio: "Updated bio2",
            AvatarImageUrl: TestConfig.S3.PublicUrl + "/updated.jpg",
            CoverImageUrl: TestConfig.S3.PublicUrl + "/updated_cover.jpg",
            Birthday: new DateOnly(1991, 2, 2),
            Gender: Gender.Female,
            HobbyIds: ["tennis".ToDeterministicGuid()]
        );
        var secondResponse = await Factory.ApiClient
            .PatchAsync<PatchUserProfileRequest, ApiResult.EmptyResult>(ApiRoutes.UserProfile.Profile, secondRequest);
        secondResponse.Success.ShouldBeTrue();

        // 4. Check response
        var getResponse =
            await Factory.ApiClient.GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);
        getResponse.Success.ShouldBeTrue();
        getResponse.Result.UserProfile.ShouldSatisfyAllConditions(
            p => p.Id.ShouldBe(authResponse.UserId),
            p => p.FirstName.ShouldBe(secondRequest.FirstName),
            p => p.LastName.ShouldBe(secondRequest.LastName),
            p => p.Bio.ShouldBe(secondRequest.Bio),
            p => p.AvatarImageUrl.ShouldBeNewerOrTheSameS3Url(secondRequest.AvatarImageUrl!),
            p => p.CoverImageUrl.ShouldBeNewerOrTheSameS3Url(secondRequest.CoverImageUrl!),
            p => p.Birthday.ShouldBe(secondRequest.Birthday!.Value),
            p => p.Gender.ShouldBe(secondRequest.Gender!.Value),
            p => p.Hobbies.Select(h => h.Id).ShouldBe(secondRequest.HobbyIds, ignoreOrder: true)
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
            p => p.Id.ShouldBe(authResponse.UserId),
            p => p.FirstName.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.FirstName),
            p => p.LastName.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.LastName),
            p => p.Bio.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Bio),
            p => p.AvatarImageUrl.ShouldBeNewerOrTheSameS3Url(request.AvatarImageUrl!),
            p => p.CoverImageUrl.ShouldBeNewerOrTheSameS3Url(getBeforeUpdateResponse.Result.UserProfile.CoverImageUrl),
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
            p => p.Id.ShouldBe(authResponse.UserId),
            p => p.FirstName.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.FirstName),
            p => p.LastName.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.LastName),
            p => p.Bio.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Bio),
            p => p.AvatarImageUrl
                .ShouldBeNewerOrTheSameS3Url(getBeforeUpdateResponse.Result.UserProfile.AvatarImageUrl),
            p => p.CoverImageUrl.ShouldBeNewerOrTheSameS3Url(request.CoverImageUrl!),
            p => p.Birthday.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Birthday),
            p => p.Gender.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Gender),
            p => p.Hobbies.ShouldBe(getBeforeUpdateResponse.Result.UserProfile.Hobbies, ignoreOrder: true)
        );
    }

    [Fact]
    public async Task GivenExistingProfile_WhenUserUpdatesPrivacy_ThenIsPublicUpdated()
    {
        // 1. Sign in and create profile
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Get profile before update
        var getBeforeUpdateResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);
        getBeforeUpdateResponse.Result.UserProfile.IsPublic.ShouldBeTrue(); // default is true

        // 3. Update privacy
        var request = new UpdatePrivacyRequest(IsPublic: false);
        var response = await Factory.ApiClient.PostAsync<UpdatePrivacyRequest, ApiResult.EmptyResult>(
            ApiRoutes.UserProfile.Privacy, request);
        response.Success.ShouldBeTrue();

        // 4. Check profile publicity
        var getAfterUpdateResponse = await Factory.ApiClient
            .GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);
        getAfterUpdateResponse.Success.ShouldBeTrue();
        getAfterUpdateResponse.Result.UserProfile.IsPublic.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenNoProfile_WhenUserGetsProfile_ThenReturnsNotFound()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get profile
        var response =
            await Factory.ApiClient.GetAsync<ApiResult<GetUserProfileResponse>>(ApiRoutes.UserProfile.Profile);

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
        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.UserProfile.AvatarUploadUrl,
            new GetPresignedUrlRequest(ContentLength: Validation.MaxAvatarSizeBytes));

        // 3. Check response
        response.Success.ShouldBeTrue();
        response.Result.UploadUrl.ShouldNotBeNullOrEmpty();
        response.Result.UploadUrl.ShouldContain("test-bucket");
        response.Result.UploadUrl.ShouldNotContain("?v=");
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserGetsCoverUploadUrl_ThenReturnsUrl()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.UserProfile.CoverUploadUrl,
            new GetPresignedUrlRequest(ContentLength: Validation.MaxCoverSizeBytes));

        // 3. Check response
        response.Success.ShouldBeTrue();
        response.Result.UploadUrl.ShouldNotBeNullOrEmpty();
        response.Result.UploadUrl.ShouldContain("test-bucket");
        response.Result.UploadUrl.ShouldNotContain("?v=");
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserGetsAvatarUploadUrlForTooLargeFile_ThenReturnsError()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.UserProfile.AvatarUploadUrl,
            new GetPresignedUrlRequest(ContentLength: Validation.MaxAvatarSizeBytes + 1));

        // 3. Check response
        response.Success.ShouldBeFalse();
        response.Result.PublicDownloadUrl.ShouldBeNull();
        response.Result.UploadUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserGetsCoverUploadUrlForTooLargeFile_ThenReturnsError()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.UserProfile.CoverUploadUrl,
            new GetPresignedUrlRequest(ContentLength: Validation.MaxCoverSizeBytes + 1));

        // 3. Check response
        response.Success.ShouldBeFalse();
        response.Result.PublicDownloadUrl.ShouldBeNull();
        response.Result.UploadUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GivenAuthenticatedUser_WhenUserUploadsImageToS3_ThenTheyCanDownloadIt()
    {
        // 1. Sign in
        var authResponse = await Factory.AuthManager.CreateAndLoginUserAsync("test@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authResponse);

        // 2. Get upload url
        var imageToUpload = "dummy image content"u8.ToArray();

        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.UserProfile.AvatarUploadUrl, new GetPresignedUrlRequest(ContentLength: imageToUpload.Length));

        // 3. Check response
        response.Success.ShouldBeTrue();
        var uploadUrl = response.Result.UploadUrl;
        uploadUrl.ShouldNotBeNullOrEmpty();
        var downloadUrl = response.Result.PublicDownloadUrl;
        downloadUrl.ShouldNotBeNullOrEmpty();

        // 4. Upload dummy image
        var uploadResponse = await Factory.S3Client.UploadAsync(uploadUrl, imageToUpload, "image/jpeg");
        uploadResponse.EnsureSuccessStatusCode();

        // 5. Verify in Minio (S3)
        var downloadedImage = await Factory.S3Client.Download(downloadUrl);
        downloadedImage.ShouldBe(imageToUpload);
    }
}