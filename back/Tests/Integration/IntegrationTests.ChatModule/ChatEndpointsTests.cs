using System.Net;
using ChatModule.API.Dto;
using ChatModule.Application.Messaging.Location;
using ChatModule.Domain.Aggregates;
using FeedModule.API.Dto;
using IntegrationTests.Shared;
using LocationModule.API.Dto;
using LocationModule.Domain.Aggregates;
using LocationModule.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Transport.Push;
using Polly;
using SecurityModule.API.Dto;
using SharedCore.Exceptions;
using Shouldly;
using Wolverine.Tracking;

namespace IntegrationTests.ChatModule;

[Collection("IntegrationTests")]
public class ChatEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public ChatEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    [Fact]
    public async Task GivenNoActiveChat_WhenUsersInviteEachOther_ThenChatIsCreatedWithCorrectExpirationDate()
    {
        // 1. Create two users with profiles
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("userA@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("userB@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        //2. Create device tokens for push notifications
        var tokenRequestA = new UpdateFcmTokenRequest("devA", "tokenA");
        Factory.ApiClient.SetAuth(authA);
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestA);

        var tokenRequestB = new UpdateFcmTokenRequest("devB", "tokenB");
        Factory.ApiClient.SetAuth(authB);
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestB);

        // 3. Verify no active chat exists for User A
        Factory.ApiClient.SetAuth(authA);
        var chatErrorBefore = await Factory.ApiClient
            .GetProblemAsync(ApiRoutes.Chat.ChatRoot);
        chatErrorBefore.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        chatErrorBefore.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));

        var inviteAExpiresAt = DateTimeOffset.UtcNow.AddHours(12);
        var inviteBExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                // 4. User A invites User B
                await Factory.ApiClient
                    .PostAsync<InviteUserRequest>(ApiRoutes.Feed.InviteUser(authB.UserId),
                        new InviteUserRequest(inviteAExpiresAt));

                // 5. User B invites User A (creates chat)
                Factory.ApiClient.SetAuth(authB);
                await Factory.ApiClient
                    .PostAsync<InviteUserRequest>(ApiRoutes.Feed.InviteUser(authA.UserId),
                        new InviteUserRequest(inviteBExpiresAt));
            },
            expectedCount: 2,
            expectedType: FcmPushBuilder.PushType.MeetingArranged);

        // 6. Verify chat exists for User A
        Factory.ApiClient.SetAuth(authA);
        var chatResponseAfter = await Factory.ApiClient
            .GetAsync<GetChatResponse>(ApiRoutes.Chat.ChatRoot);
        chatResponseAfter.Participant.Id.ShouldBe(Guid.Parse(authB.UserId));
        chatResponseAfter.ParticipantsMet.ShouldBeFalse();

        // 7. Verify chat exists for User B
        Factory.ApiClient.SetAuth(authB);
        var chatResponseB = await Factory.ApiClient
            .GetAsync<GetChatResponse>(ApiRoutes.Chat.ChatRoot);
        chatResponseB.Participant.Id.ShouldBe(Guid.Parse(authA.UserId));
        chatResponseB.ExpiresAt.ShouldBe(inviteAExpiresAt);
    }

    [Fact]
    public async Task GivenActiveChat_WhenUsersMetInLocation_ThenParticipantsMetFlagIsTrue()
    {
        // 1. Create two users and establish chat
        var (authA, authB) = await CreateTwoUsersWithChatAsync();

        // 2. Wait while tracked users are saved to the database
        using var scope = Factory.Services.CreateScope();
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
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.FakeTimeProvider.SetUtcNow(DateTime.UtcNow + TimeSpan.FromHours(1));

                Factory.ApiClient.SetAuth(authA);
                await Factory.ApiClient.PostAsync<UpdateLocationRequest>(
                    ApiRoutes.Location.PostLocation,
                    new UpdateLocationRequest(Latitude: 0.0, Longitude: 0.0));

                Factory.ApiClient.SetAuth(authB);
                await Factory.ApiClient.PostAsync<UpdateLocationRequest>(
                    ApiRoutes.Location.PostLocation,
                    new UpdateLocationRequest(Latitude: 0.0, Longitude: 0.0));
            },
            expectedCount: 2,
            expectedType: FcmPushBuilder.PushType.ShowEndMeetingSuggestion);

        // 4. Verify ParticipantsMet flag is true
        Factory.ApiClient.SetAuth(authA);
        var chatResponse = await Factory.ApiClient
            .GetAsync<GetChatResponse>(ApiRoutes.Chat.ChatRoot);
        chatResponse.ParticipantsMet.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenActiveChat_WhenUserDeletesChat_ThenFeedStatusReturnsUserIdToRate()
    {
        // 1. Create two users and establish chat
        var (authA, authB) = await CreateTwoUsersWithChatAsync();

        // 2. Verify IsInMeeting is true before deletion
        Factory.ApiClient.SetAuth(authA);
        var statusBefore = await Factory.ApiClient
            .GetAsync<GetFeedStatusResponse>(ApiRoutes.Feed.GetStatus);
        statusBefore.IsInMeeting.ShouldBeTrue();

        // 3. User A deletes the chat, and we are waiting for delivery of pushes to both users
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                var deleteResponse = await Factory.ApiClient
                    .DeleteAsync(ApiRoutes.Chat.ChatRoot);
            },
            expectedCount: 2,
            expectedType: FcmPushBuilder.PushType.OpenRateUserScreen);

        // 4. Verify chat is deleted for User A
        var chatErrorAfterDelete = await Factory.ApiClient
            .GetProblemAsync(ApiRoutes.Chat.ChatRoot);
        chatErrorAfterDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        chatErrorAfterDelete.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));

        // 5. Verify FeedStatus returns UserIdToRate for User A
        var statusAfterA = await Factory.ApiClient
            .GetAsync<GetFeedStatusResponse>(ApiRoutes.Feed.GetStatus);
        statusAfterA.IsInMeeting.ShouldBeFalse();
        statusAfterA.UserIdToRate.ShouldBe(Guid.Parse(authB.UserId));

        // 6. Verify FeedStatus returns UserIdToRate for User B
        Factory.ApiClient.SetAuth(authB);
        var statusAfterB = await Factory.ApiClient
            .GetAsync<GetFeedStatusResponse>(ApiRoutes.Feed.GetStatus);
        statusAfterB.IsInMeeting.ShouldBeFalse();
        statusAfterB.UserIdToRate.ShouldBe(Guid.Parse(authA.UserId));

        // 7. Verify that UserIdToRate is null after rating updated
        var rateRequest = new UserRatingDto(4.5m);
        await Factory.ApiClient.PostAsync<UserRatingDto>(
            ApiRoutes.Feed.RateUser(authA.UserId), rateRequest);
        var statusAfterRate = await Factory.ApiClient
            .GetAsync<GetFeedStatusResponse>(ApiRoutes.Feed.GetStatus);
        statusAfterRate.UserIdToRate.ShouldBeNull();
    }

    [Fact]
    public async Task GivenActiveChat_WhenUsersExchangeMessages_ThenBothReceiveSilentPushesAndChatHistory()
    {
        // 1. Create two users and establish chat
        var (authA, authB) = await CreateTwoUsersWithChatAsync();

        // 2. User A sends message to User B
        const string messageA = "Hello User B!";
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authA);
                var request = new SendMessageRequest(messageA);
                var response = await Factory.ApiClient
                    .PostAsync<SendMessageRequest>(ApiRoutes.Chat.Message, request);
            },
            expectedCount: 1,
            pushPredicate: p =>
                p.UnicastMessage!.Data[FcmPushBuilder.Keys.Type] == FcmPushBuilder.PushType.ChatMessageReceived &&
                p.UnicastMessage!.Data[FcmPushBuilder.Keys.MessageText] == messageA);

        // 3. User B sends message to User A
        const string messageB = "Message back to User A";
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(authB);
                var request = new SendMessageRequest(messageB);
                var response = await Factory.ApiClient
                    .PostAsync<SendMessageRequest>(ApiRoutes.Chat.Message, request);
            },
            expectedCount: 1,
            pushPredicate: p =>
                p.UnicastMessage!.Data[FcmPushBuilder.Keys.Type] == FcmPushBuilder.PushType.ChatMessageReceived &&
                p.UnicastMessage!.Data[FcmPushBuilder.Keys.MessageText] == messageB);

        // 4. Verify User A sees both messages in history
        Factory.ApiClient.SetAuth(authA);
        var chatResponseA = await Factory.ApiClient
            .GetAsync<GetChatResponse>(ApiRoutes.Chat.ChatRoot);
        chatResponseA.Messages.Count.ShouldBe(2);
        chatResponseA.Messages[0].MessageText.ShouldBe(messageA);
        chatResponseA.Messages[0].IsMine.ShouldBeTrue();
        chatResponseA.Messages[1].MessageText.ShouldBe(messageB);
        chatResponseA.Messages[1].IsMine.ShouldBeFalse();

        // 5. Verify User B sees both messages in history
        Factory.ApiClient.SetAuth(authB);
        var chatResponseB = await Factory.ApiClient
            .GetAsync<GetChatResponse>(ApiRoutes.Chat.ChatRoot);
        chatResponseB.Messages.Count.ShouldBe(2);
        chatResponseB.Messages[0].MessageText.ShouldBe(messageA);
        chatResponseB.Messages[0].IsMine.ShouldBeFalse();
        chatResponseB.Messages[1].MessageText.ShouldBe(messageB);
        chatResponseB.Messages[1].IsMine.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenActiveChat_WhenUserSendsMoreThan30Messages_ThenReturnsError()
    {
        // 1. Create two users and establish chat
        var (authA, _) = await CreateTwoUsersWithChatAsync();

        // 2. User A sends MaxMessagesPerUser messages successfully
        Factory.ApiClient.SetAuth(authA);
        for (int i = 1; i <= ValidationParameters.MaxMessagesPerUser; i++)
        {
            var request = new SendMessageRequest($"Message {i}");
            var response = await Factory.ApiClient
                .PostAsync<SendMessageRequest>(ApiRoutes.Chat.Message, request);
        }

        // 3. When user A sends MaxMessagesPerUser + 1 message then receives an error
        var requestExtra = new SendMessageRequest("Extra message");
        var errorExtra = await Factory.ApiClient
            .PostProblemAsync(ApiRoutes.Chat.Message, requestExtra);

        errorExtra.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        errorExtra.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.UnprocessableEntity);
        errorExtra.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.ValidationError));
        errorExtra.ProblemDetails.Detail.ShouldStartWith("You are unable to post more than");
    }

    [Fact]
    public async Task GivenActiveChat_WhenUserDeletesAccount_ThenAnotherUserCanEstablishNewChat()
    {
        // 1. Create two users and establish chat
        var (authA, authB) = await CreateTwoUsersWithChatAsync();

        // 2. Delete account B
        Factory.ApiClient.SetAuth(authB);
        await Factory.Host.ExecuteAndWaitAsync(async () =>
        {
            var deleteResponse = await Factory.ApiClient.DeleteAsync(ApiRoutes.Auth.Account);
        });

        // 3. Create account C
        var authC = await Factory.AuthManager.CreateAndLoginUserAsync("chatUserC@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authC);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var tokenRequestC = new UpdateFcmTokenRequest("devC", "tokenC");
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestC);

        // 4. User A and C establishes a new chat
        var inviteExpiresAt = Factory.FakeTimeProvider.GetUtcNow().AddHours(4);
        await Factory.Host.ExecuteAndWaitAsync(async () =>
        {
            // User A invites User C
            Factory.ApiClient.SetAuth(authA);
            await Factory.ApiClient
                .PostAsync<InviteUserRequest>(
                    ApiRoutes.Feed.InviteUser(authC.UserId),
                    new InviteUserRequest(inviteExpiresAt)
                );

            // User B invites User A (creates chat)
            Factory.ApiClient.SetAuth(authC);
            await Factory.ApiClient
                .PostAsync<InviteUserRequest>(
                    ApiRoutes.Feed.InviteUser(authA.UserId),
                    new InviteUserRequest(inviteExpiresAt)
                );
        });

        // 5. Check chat existence
        Factory.ApiClient.SetAuth(authA);

        var statusA = await Factory.ApiClient
            .GetAsync<GetFeedStatusResponse>(ApiRoutes.Feed.GetStatus);

        statusA.IsInMeeting.ShouldBeTrue();

        var chatResponseA = await Factory.ApiClient
            .GetAsync<GetChatResponse>(ApiRoutes.Chat.ChatRoot);

        Factory.ApiClient.SetAuth(authC);

        var statusC = await Factory.ApiClient
            .GetAsync<GetFeedStatusResponse>(ApiRoutes.Feed.GetStatus);
        statusC.IsInMeeting.ShouldBeTrue();

        await Factory.ApiClient
            .GetAsync<GetChatResponse>(ApiRoutes.Chat.ChatRoot);
    }

    private async Task<(AuthResponse authA, AuthResponse authB)>
        CreateTwoUsersWithChatAsync()
    {
        // Create User A
        var authA = await Factory.AuthManager.CreateAndLoginUserAsync("chatUserA@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authA);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // Create User B
        var authB = await Factory.AuthManager.CreateAndLoginUserAsync("chatUserB@example.com", "Password123!");
        Factory.ApiClient.SetAuth(authB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        // Create device tokens for push notifications
        var tokenRequestA = new UpdateFcmTokenRequest("devA", "tokenA");
        Factory.ApiClient.SetAuth(authA);
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestA);

        var tokenRequestB = new UpdateFcmTokenRequest("devB", "tokenB");
        Factory.ApiClient.SetAuth(authB);
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest>(ApiRoutes.Notifications.UpdateDeviceToken,
                tokenRequestB);

        // Await chat creation
        var inviteExpiresAt = Factory.FakeTimeProvider.GetUtcNow().AddHours(4);
        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                // User A invites User B
                Factory.ApiClient.SetAuth(authA);
                await Factory.ApiClient
                    .PostAsync<InviteUserRequest>(
                        ApiRoutes.Feed.InviteUser(authB.UserId),
                        new InviteUserRequest(inviteExpiresAt)
                    );

                // User B invites User A (creates chat)
                Factory.ApiClient.SetAuth(authB);
                await Factory.ApiClient
                    .PostAsync<InviteUserRequest>(
                        ApiRoutes.Feed.InviteUser(authA.UserId),
                        new InviteUserRequest(inviteExpiresAt)
                    );
            },
            expectedCount: 2,
            expectedType: FcmPushBuilder.PushType.MeetingArranged);

        return (authA, authB);
    }
}