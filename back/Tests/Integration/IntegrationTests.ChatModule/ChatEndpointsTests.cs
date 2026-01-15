using ChatModule.API.Dto;
using ChatModule.Application.Messaging.Location;
using ChatModule.Domain.Aggregates;
using FeedModule.API.Dto;
using IntegrationTests.Shared;
using LocationModule.API.Dto;
using LocationModule.Domain.Aggregates;
using LocationModule.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NotificationsModule.API;
using NotificationsModule.Infrastructure.Transport.Push;
using Polly;
using SecurityModule.API.Dto;
using SharedCore.Api.Dto;
using Shouldly;

namespace IntegrationTests.ChatModule;

[Collection("IntegrationTests")]
public class ChatEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationWebApplicationFactory _factory;

    public ChatEndpointsTests(IntegrationWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.ResetDatabaseAsync();

    [Fact]
    public async Task GivenNoActiveChat_WhenUsersInviteEachOther_ThenChatIsCreated()
    {
        // 1. Create two users with profiles
        var authA = await _factory.AuthManager.CreateAndLoginUserAsync("userA@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await _factory.AuthManager.CreateAndLoginUserAsync("userB@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // 2. Verify no active chat exists for User A
        _factory.ApiClient.SetAuth(authA);
        var chatResponseBefore = await _factory.ApiClient
            .GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatResponseBefore.Success.ShouldBeFalse();

        // 3. User A invites User B
        var inviteResponseA = await _factory.ApiClient
            .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authB.UserId), null);
        inviteResponseA.Success.ShouldBeTrue();

        // 4. User B invites User A (creates chat)
        _factory.ApiClient.SetAuth(authB);
        var inviteResponseB = await _factory.ApiClient
            .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authA.UserId), null);
        inviteResponseB.Success.ShouldBeTrue();

        // 5. Verify chat exists for User A
        _factory.ApiClient.SetAuth(authA);
        var chatResponseAfter = await _factory.ApiClient
            .GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatResponseAfter.Success.ShouldBeTrue();
        chatResponseAfter.Result.Participant.Id.ShouldBe(Guid.Parse(authB.UserId));
        chatResponseAfter.Result.ParticipantsMet.ShouldBeFalse();

        // 6. Verify chat exists for User B
        _factory.ApiClient.SetAuth(authB);
        var chatResponseB = await _factory.ApiClient
            .GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatResponseB.Success.ShouldBeTrue();
        chatResponseB.Result.Participant.Id.ShouldBe(Guid.Parse(authA.UserId));
    }

    [Fact]
    public async Task GivenActiveChat_WhenUsersMetInLocation_ThenParticipantsMetFlagIsTrue()
    {
        // 1. Create two users and establish chat
        var (authA, authB) = await CreateTwoUsersWithChatAsync();

        // 2. Wait while tracked users are saved to the database
        using var scope = _factory.Services.CreateScope();
        var trackedUserPairRepository = scope.ServiceProvider.GetService<ITrackedUserPairRepository>();

        var retryPolicy = Policy
            .HandleResult<TrackedUserPair[]>(result => result.Length == 0)
            .WaitAndRetryAsync(retryCount: 3, sleepDurationProvider: _ => TimeSpan.FromMilliseconds(500));
        await retryPolicy.ExecuteAsync(async () =>
            await trackedUserPairRepository!.GetByUserIdAsync(
                Guid.Parse(authA.UserId)
            )
        );

        // 3. Update users' locations and wait for silent pushes
        ChatParticipantsMetEventHandler.MetEventDelay = TimeSpan.FromMilliseconds(100);
        await _factory.NotificationManager.AwaitSilentPushNotifications(
            codeToRun: async () =>
            {
                _factory.FakeTimeProvider.SetUtcNow(DateTime.UtcNow + TimeSpan.FromHours(1));

                _factory.ApiClient.SetAuth(authA);
                var locationA = await _factory.ApiClient.PostAsync<UpdateLocationRequest, ApiResult.EmptyResult>(
                    ApiRoutes.Location.PostLocation,
                    new UpdateLocationRequest(Latitude: 0.0, Longitude: 0.0));
                locationA.Success.ShouldBeTrue();

                _factory.ApiClient.SetAuth(authB);
                var locationB = await _factory.ApiClient.PostAsync<UpdateLocationRequest, ApiResult.EmptyResult>(
                    ApiRoutes.Location.PostLocation,
                    new UpdateLocationRequest(Latitude: 0.0, Longitude: 0.0));
                locationB.Success.ShouldBeTrue();
            },
            expectedCount: 2,
            expectedAction: FcmPushBuilder.Actions.ShowEndMeetingSuggestion);

        // 4. Verify ParticipantsMet flag is true
        _factory.ApiClient.SetAuth(authA);
        var chatResponse = await _factory.ApiClient
            .GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatResponse.Success.ShouldBeTrue();
        chatResponse.Result.ParticipantsMet.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenActiveChat_WhenUserDeletesChat_ThenFeedStatusReturnsUserIdToRate()
    {
        // 1. Create two users and establish chat
        var (authA, authB) = await CreateTwoUsersWithChatAsync();

        // 2. Verify IsInMeeting is true before deletion
        _factory.ApiClient.SetAuth(authA);
        var statusBefore = await _factory.ApiClient
            .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusBefore.Success.ShouldBeTrue();
        statusBefore.Result.IsInMeeting.ShouldBeTrue();

        // 3. User A deletes the chat, and we are waiting for delivery of pushes to both users
        await _factory.NotificationManager.AwaitSilentPushNotifications(
            codeToRun: async () =>
            {
                var deleteResponse = await _factory.ApiClient
                    .DeleteAsync<ApiResult.EmptyResult>(ApiRoutes.Chat.ChatRoot);
                deleteResponse.Success.ShouldBeTrue();
            },
            expectedCount: 2,
            expectedAction: FcmPushBuilder.Actions.OpenRateUserScreen);

        // 4. Verify chat is deleted for User A
        var chatAfterDelete = await _factory.ApiClient
            .GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatAfterDelete.Success.ShouldBeFalse();

        // 5. Verify FeedStatus returns UserIdToRate for User A
        var statusAfterA = await _factory.ApiClient
            .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusAfterA.Success.ShouldBeTrue();
        statusAfterA.Result.IsInMeeting.ShouldBeFalse();
        statusAfterA.Result.UserIdToRate.ShouldBe(Guid.Parse(authB.UserId));

        // 6. Verify FeedStatus returns UserIdToRate for User B
        _factory.ApiClient.SetAuth(authB);
        var statusAfterB = await _factory.ApiClient
            .GetAsync<ApiResult<GetFeedStatusResponse>>(ApiRoutes.Feed.GetStatus);
        statusAfterB.Success.ShouldBeTrue();
        statusAfterB.Result.IsInMeeting.ShouldBeFalse();
        statusAfterB.Result.UserIdToRate.ShouldBe(Guid.Parse(authA.UserId));
    }


    [Fact]
    public async Task GivenActiveChat_WhenUsersExchangeMessages_ThenBothReceiveSilentPushesAndChatHistory()
    {
        // 1. Create two users and establish chat
        var (authA, authB) = await CreateTwoUsersWithChatAsync();

        // 2. User A sends message to User B
        const string messageA = "Hello User B!";
        await _factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                _factory.ApiClient.SetAuth(authA);
                var request = new SendMessageRequest(messageA);
                var response = await _factory.ApiClient
                    .PostAsync<SendMessageRequest, ApiResult.EmptyResult>(ApiRoutes.Chat.Message, request);
                response.Success.ShouldBeTrue();
            },
            expectedCount: 1,
            pushPredicate: p =>
                p.UnicastMessage!.Data[FcmPushBuilder.Keys.UserAction] == FcmPushBuilder.UserActions.OpenChatScreen &&
                p.UnicastMessage!.Data[FcmPushBuilder.Keys.MessageText] == messageA);

        // 3. User B sends message to User A
        const string messageB = "Message back to User A";
        await _factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                _factory.ApiClient.SetAuth(authB);
                var request = new SendMessageRequest(messageB);
                var response = await _factory.ApiClient
                    .PostAsync<SendMessageRequest, ApiResult.EmptyResult>(ApiRoutes.Chat.Message, request);
                response.Success.ShouldBeTrue();
            },
            expectedCount: 1,
            pushPredicate: p =>
                p.UnicastMessage!.Data[FcmPushBuilder.Keys.UserAction] == FcmPushBuilder.UserActions.OpenChatScreen &&
                p.UnicastMessage!.Data[FcmPushBuilder.Keys.MessageText] == messageB);

        // 4. Verify User A sees both messages in history
        _factory.ApiClient.SetAuth(authA);
        var chatResponseA = await _factory.ApiClient
            .GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatResponseA.Success.ShouldBeTrue();
        chatResponseA.Result.Messages.Count.ShouldBe(2);
        chatResponseA.Result.Messages[0].MessageText.ShouldBe(messageA);
        chatResponseA.Result.Messages[0].IsMine.ShouldBeTrue();
        chatResponseA.Result.Messages[1].MessageText.ShouldBe(messageB);
        chatResponseA.Result.Messages[1].IsMine.ShouldBeFalse();

        // 5. Verify User B sees both messages in history
        _factory.ApiClient.SetAuth(authB);
        var chatResponseB = await _factory.ApiClient
            .GetAsync<ApiResult<GetChatResponse>>(ApiRoutes.Chat.ChatRoot);
        chatResponseB.Success.ShouldBeTrue();
        chatResponseB.Result.Messages.Count.ShouldBe(2);
        chatResponseB.Result.Messages[0].MessageText.ShouldBe(messageA);
        chatResponseB.Result.Messages[0].IsMine.ShouldBeFalse();
        chatResponseB.Result.Messages[1].MessageText.ShouldBe(messageB);
        chatResponseB.Result.Messages[1].IsMine.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenActiveChat_WhenUserSendsMoreThan30Messages_ThenReturnsError()
    {
        // 1. Create two users and establish chat
        var (authA, _) = await CreateTwoUsersWithChatAsync();

        // 2. User A sends MaxMessagesPerUser messages successfully
        _factory.ApiClient.SetAuth(authA);
        for (int i = 1; i <= ValidationParameters.MaxMessagesPerUser; i++)
        {
            var request = new SendMessageRequest($"Message {i}");
            var response = await _factory.ApiClient
                .PostAsync<SendMessageRequest, ApiResult.EmptyResult>(ApiRoutes.Chat.Message, request);
            response.Success.ShouldBeTrue();
        }

        // 3. When user A sends MaxMessagesPerUser + 1 message then receives an error
        var requestExtra = new SendMessageRequest("Extra message");
        var responseExtra = await _factory.ApiClient
            .PostAsync<SendMessageRequest, ApiResult.EmptyResult>(ApiRoutes.Chat.Message, requestExtra);

        responseExtra.Success.ShouldBeFalse();
        responseExtra.Errors[0].Error.ShouldStartWith("You are unable to post more than");
    }

    private async Task<(AuthResponse authA, AuthResponse authB)>
        CreateTwoUsersWithChatAsync()
    {
        // Create User A
        var authA = await _factory.AuthManager.CreateAndLoginUserAsync("chatUserA@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authA);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // Create User B
        var authB = await _factory.AuthManager.CreateAndLoginUserAsync("chatUserB@example.com", "Password123!");
        _factory.ApiClient.SetAuth(authB);
        await _factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // Create device tokens for push notifications
        var tokenRequestA = new UpdateFcmTokenRequest("devA", "tokenA");
        _factory.ApiClient.SetAuth(authA);
        await _factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestA);

        var tokenRequestB = new UpdateFcmTokenRequest("devB", "tokenB");
        _factory.ApiClient.SetAuth(authB);
        await _factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestB);

        // Await chat creation
        await _factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                // User A invites User B
                _factory.ApiClient.SetAuth(authA);
                await _factory.ApiClient
                    .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authB.UserId), null);

                // User B invites User A (creates chat)
                _factory.ApiClient.SetAuth(authB);
                await _factory.ApiClient
                    .PostAsync<object?, ApiResult.EmptyResult>(ApiRoutes.Feed.InviteUser(authA.UserId), null);
            },
            expectedCount: 2,
            expectedTitle: "Invitation accepted!");

        return (authA, authB);
    }
}