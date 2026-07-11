using System.Net;
using System.Net.Http.Json;
using EventModule.API.B2C.Dto;
using EventModule.Domain.Enums;
using EventModule.Domain.Services;
using EventModule.Infrastructure.Persistence;
using IntegrationTests.Shared;
using IntegrationTests.Shared.HttpClients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationModule.Domain.ValueObjects;
using NotificationsModule.API.Dto;
using NotificationsModule.Infrastructure.Transport.Push;
using SecurityModule.API.Dto;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.ValueObjects;
using SharedCore.Api.Dto;
using SharedCore.Enums;
using SharedCore.Exceptions;
using SharedCore.Extensions;
using Shouldly;
using VenueModule.API.B2B.Companies.Dto;
using VenueModule.API.B2B.Venues.Dto;

namespace IntegrationTests.EventModule;

[Collection("IntegrationTests")]
public class B2CEventEndpointsTests : IntegrationTestBase, IAsyncLifetime
{
    public B2CEventEndpointsTests(IntegrationWebApplicationFactory factory) : base(factory)
    {
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => ContainerManager.ResetDatabaseAsync();

    // ========================================
    //  CREATE EVENT
    // ========================================

    [Fact]
    public async Task GivenUser_WhenCreatesMeetup_ThenCreated()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var futureDate = DateTimeOffset.UtcNow.AddDays(14);
        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Fun Meetup",
                Description: "A great gathering",
                StartDateTime: futureDate,
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: false));

        response.Success.ShouldBeTrue();
        response.Result.ShouldNotBeNull();
        response.Result.Title.ShouldBe("Fun Meetup");
        response.Result.Description.ShouldBe("A great gathering");
        response.Result.Type.ShouldBe(EventType.Meetup);
        response.Result.IsCreator.ShouldBeTrue();
        response.Result.CreatorId.ShouldBe(auth.UserId);
        response.Result.ParticipationStatus.ShouldBe("Approved");
        response.Result.ParticipantsCount.ShouldBe(1);
        response.Result.RequiresApproval.ShouldBeFalse();
        response.Result.IsVisibleOnMap.ShouldBeTrue();
        Guid.TryParse(response.Result.Id, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithContactsAndHobbies_ThenCreated()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var hobbyId = "boxing".ToDeterministicGuid();
        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Meetup with extras",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: false,
                Contacts:
                [
                    new CreateEventContactDto(ContactType.Telegram, "testuser", IsPublic: true),
                    new CreateEventContactDto(ContactType.Email, "test@test.com", IsPublic: false)
                ],
                HobbyIds: [hobbyId]));

        response.Success.ShouldBeTrue();
        response.Result.Contacts.Length.ShouldBe(2);
        response.Result.Contacts.ShouldContain(c => c.Type == ContactType.Telegram && c.Value == "testuser");
        response.Result.HobbyIds.ShouldContain(hobbyId);
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithRequiresApproval_ThenCreated()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Approval Meetup",
                Description: "Need approval",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: true));

        response.Success.ShouldBeTrue();
        response.Result.RequiresApproval.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithMaxParticipants_ThenCreated()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Limited Meetup",
                Description: "Only 5 spots",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid(),
                MaxParticipants: 5,
                RequiresApproval: false));

        response.Success.ShouldBeTrue();
        response.Result.MaxParticipants.ShouldBe(5);
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithEmptyTitle_Then422()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid()));

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithTitleTooLong_Then422()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: new string('x', 201),
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid()));

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithDescriptionTooLong_Then422()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Valid Title",
                Description: new string('x', 2001),
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid()));

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenUnauthenticated_WhenCreates_Then401()
    {
        var invalidAuth = new AuthResponse(
            UserId: "",
            UserType: UserType.B2C,
            RequiredAction: RequiredAction.None,
            AccessToken: null,
            RefreshToken: null
        );
        Factory.HttpClient.SetAuth(invalidAuth);
        var httpResponse = await Factory.HttpClient.PostAsJsonAsync(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Hacked Meetup",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid()));

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ========================================
    //  GET EVENT
    // ========================================

    [Fact]
    public async Task GivenExistingEvent_WhenGet_ThenReturnsDetail()
    {
        var (auth, eventId) = await CreateEventAsync();

        Factory.ApiClient.SetAuth(auth);
        var response = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(ApiRoutes.Event.Single(eventId));

        response.Success.ShouldBeTrue();
        response.Result.Id.ShouldBe(eventId.ToString());
        response.Result.Title.ShouldBe("Test Meetup");
        response.Result.IsCreator.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenNonExistentEvent_WhenGet_Then404()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(Guid.NewGuid()));

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenGet_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(ApiRoutes.Event.Single(eventId));

        var response = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(ApiRoutes.Event.Single(eventId));

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenExistingEvent_WhenGetByNonCreator_ThenReturnsDetail()
    {
        var (_, eventId) = await CreateEventAsync();
        var otherUser = await CreateUserAsync();

        Factory.ApiClient.SetAuth(otherUser);
        var response = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(ApiRoutes.Event.Single(eventId));

        response.Success.ShouldBeTrue();
        response.Result.IsCreator.ShouldBeFalse();
        response.Result.Id.ShouldBe(eventId.ToString());
    }

    // ========================================
    //  UPDATE EVENT
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenUpdatesTitleAndDescription_ThenUpdated()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PatchAsync<UpdateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId),
            new UpdateEventRequest(
                Title: "Updated Title",
                Description: "Updated Desc",
                StartDateTime: null,
                IsVisibleOnMap: null,
                MaxParticipants: null,
                RequiresApproval: null,
                CoverImageUrl: null,
                Contacts: null,
                HobbyIds: null));

        response.Success.ShouldBeTrue();
        response.Result.Title.ShouldBe("Updated Title");
        response.Result.Description.ShouldBe("Updated Desc");
    }

    [Fact]
    public async Task GivenCreator_WhenUpdatesContacts_ThenReplaced()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PatchAsync<UpdateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId),
            new UpdateEventRequest(
                Title: null,
                Description: null,
                StartDateTime: null,
                IsVisibleOnMap: null,
                MaxParticipants: null,
                RequiresApproval: null,
                CoverImageUrl: null,
                Contacts:
                [
                    new CreateEventContactDto(ContactType.Phone, "+381611111111", IsPublic: true)
                ],
                HobbyIds: null));

        response.Success.ShouldBeTrue();
        response.Result.Contacts.Length.ShouldBe(1);
        response.Result.Contacts[0].Value.ShouldBe("+381611111111");
    }

    [Fact]
    public async Task GivenCreator_WhenSetsIsVisibleOnMapToFalse_ThenUpdated()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PatchAsync<UpdateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId),
            new UpdateEventRequest(
                Title: null,
                Description: null,
                StartDateTime: null,
                IsVisibleOnMap: false,
                MaxParticipants: null,
                RequiresApproval: null,
                CoverImageUrl: null,
                Contacts: null,
                HobbyIds: null));

        response.Success.ShouldBeTrue();
        response.Result.IsVisibleOnMap.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenNonCreator_WhenUpdates_Then403()
    {
        var (_, eventId) = await CreateEventAsync();
        var otherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(otherUser);

        var response = await Factory.ApiClient.PatchAsync<UpdateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId),
            new UpdateEventRequest(
                Title: "Hacked Title",
                Description: null,
                StartDateTime: null,
                IsVisibleOnMap: null,
                MaxParticipants: null,
                RequiresApproval: null,
                CoverImageUrl: null,
                Contacts: null,
                HobbyIds: null));

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenUpdates_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(ApiRoutes.Event.Single(eventId));

        var response = await Factory.ApiClient.PatchAsync<UpdateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId),
            new UpdateEventRequest(
                Title: "After Delete",
                Description: null,
                StartDateTime: null,
                IsVisibleOnMap: null,
                MaxParticipants: null,
                RequiresApproval: null,
                CoverImageUrl: null,
                Contacts: null,
                HobbyIds: null));

        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  DELETE EVENT
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenDeletes_ThenSoftDeleted()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var deleteResponse = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.Event.Single(eventId));
        deleteResponse.Success.ShouldBeTrue();

        var getResponse = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));
        getResponse.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenNonCreator_WhenDeletes_Then403()
    {
        var (_, eventId) = await CreateEventAsync();
        var otherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(otherUser);

        var response = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.Event.Single(eventId));
        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenDeletesAgain_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(ApiRoutes.Event.Single(eventId));

        var response = await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(
            ApiRoutes.Event.Single(eventId));
        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  MAP
    // ========================================

    [Fact]
    public async Task GivenEventsWithinBounds_WhenGetMap_ThenReturnsGroupedByVenue()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueAId = await SeedVenueAsync(44.7866, 20.4489);
        var venueBId = await SeedVenueAsync(44.7870, 20.4490);

        await CreateEventAtVenueAsync(auth, venueAId);
        await CreateEventAtVenueAsync(auth, venueBId);

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, ApiResult<VenueEventsDto[]>>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 44.78,
                MaxLat: 44.80,
                MinLng: 20.44,
                MaxLng: 20.46));

        response.Success.ShouldBeTrue();
        response.Result.Length.ShouldBe(2);
        response.Result.ShouldContain(v => v.VenueId == venueAId.ToString());
        response.Result.ShouldContain(v => v.VenueId == venueBId.ToString());
        response.Result.Single(v => v.VenueId == venueAId.ToString()).Events.Length.ShouldBe(1);
    }

    [Fact]
    public async Task GivenEventsOutsideBounds_WhenGetMap_ThenEmpty()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        await CreateEventAtVenueAsync(auth, venueId);

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, ApiResult<VenueEventsDto[]>>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 45.0,
                MaxLat: 46.0,
                MinLng: 21.0,
                MaxLng: 22.0));

        response.Success.ShouldBeTrue();
        response.Result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenEmptyEventTypes_WhenGetMap_ThenEmpty()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        await CreateEventAtVenueAsync(auth, venueId);

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, ApiResult<VenueEventsDto[]>>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 44.78,
                MaxLat: 44.80,
                MinLng: 20.44,
                MaxLng: 20.46,
                EventTypes: []));

        response.Success.ShouldBeTrue();
        response.Result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenBlockedCreator_WhenGetMap_ThenBlockedEventsExcluded()
    {
        var userA = await CreateUserAsync();
        var userB = await CreateUserAsync();

        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();
        Factory.ApiClient.SetAuth(userB);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        await CreateEventAtVenueAsync(userB, venueId);

        Factory.ApiClient.SetAuth(userA);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Feed.BlockUser(userB.UserId), new { });

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, ApiResult<VenueEventsDto[]>>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 44.78,
                MaxLat: 44.80,
                MinLng: 20.44,
                MaxLng: 20.46));

        response.Success.ShouldBeTrue();
        response.Result.ShouldBeEmpty();
    }

    // ========================================
    //  VENUE EVENTS
    // ========================================

    [Fact]
    public async Task GivenVenueWithEvents_WhenGetVenueEvents_ThenReturnsPaginated()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        var eventId = await CreateEventAtVenueAsync(auth, venueId);

        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<EventListItemDto>>>(
            ApiRoutes.Event.VenueEvents(venueId) + "?Offset=0&Limit=20");

        response.Success.ShouldBeTrue();
        response.Result.Data.Count.ShouldBe(1);
        response.Result.TotalCount.ShouldBe(1);
        response.Result.Data.First().Id.ShouldBe(eventId.ToString());
    }

    [Fact]
    public async Task GivenVenueWithInvisibleEvent_WhenGetVenueEvents_ThenExcluded()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        var eventId = await CreateEventAtVenueAsync(auth, venueId);

        await Factory.ApiClient.PatchAsync<UpdateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId),
            new UpdateEventRequest(
                Title: null, Description: null, StartDateTime: null,
                IsVisibleOnMap: false, MaxParticipants: null, RequiresApproval: null,
                CoverImageUrl: null, Contacts: null, HobbyIds: null));

        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<EventListItemDto>>>(
            ApiRoutes.Event.VenueEvents(venueId) + "?Offset=0&Limit=20");

        response.Success.ShouldBeTrue();
        response.Result.Data.ShouldBeEmpty();
        response.Result.TotalCount.ShouldBe(0);
    }

    // ========================================
    //  JOIN EVENT
    // ========================================

    [Fact]
    public async Task GivenPublicEvent_WhenJoin_ThenApproved()
    {
        var (creatorAuth, eventId) = await CreateEventAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        response.Success.ShouldBeTrue();

        Factory.ApiClient.SetAuth(creatorAuth);
        var eventDetail = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.Result.ParticipantsCount.ShouldBe(2);
        eventDetail.Result.ApprovedParticipantIds.ShouldContain(Guid.Parse(joiner.UserId));
    }

    [Fact]
    public async Task GivenApprovalEvent_WhenJoin_ThenPending()
    {
        var (creatorAuth, eventId) = await CreateEventWithApprovalAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        response.Success.ShouldBeTrue();

        Factory.ApiClient.SetAuth(creatorAuth);
        var eventDetail = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.Result.PendingApprovalsCount.ShouldBe(1);
        eventDetail.Result.PendingParticipantIds.ShouldContain(Guid.Parse(joiner.UserId));
    }

    [Fact]
    public async Task GivenAlreadyJoined_WhenJoinAgain_Then409()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenPublicEventAtMaxCapacity_WhenJoin_Then409()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Full Meetup",
                Description: "Only 1 spot",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid(),
                MaxParticipants: 1,
                RequiresApproval: false));

        var eventId = Guid.Parse(createResponse.Result.Id);
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenApprovalEventWithPendingAtMax_WhenJoin_Then422()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Full Approval Meetup",
                Description: "1 spot, requires approval",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid(),
                MaxParticipants: 1,
                RequiresApproval: true));

        var eventId = Guid.Parse(createResponse.Result.Id);
        var filler = await CreateUserAsync();
        Factory.ApiClient.SetAuth(filler);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        var anotherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(anotherUser);
        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenJoin_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(ApiRoutes.Event.Single(eventId));

        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  LEAVE EVENT
    // ========================================

    [Fact]
    public async Task GivenParticipant_WhenLeave_ThenRemoved()
    {
        var (creatorAuth, eventId) = await CreateEventAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        var leaveResponse = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Leave(eventId), new { });
        leaveResponse.Success.ShouldBeTrue();

        Factory.ApiClient.SetAuth(creatorAuth);
        var eventDetail = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.Result.ParticipantsCount.ShouldBe(1);
    }

    [Fact]
    public async Task GivenNonParticipant_WhenLeave_Then409()
    {
        var (_, eventId) = await CreateEventAsync();
        var nonParticipant = await CreateUserAsync();
        Factory.ApiClient.SetAuth(nonParticipant);

        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Leave(eventId), new { });

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenCreator_WhenLeave_Then403()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Leave(eventId), new { });

        response.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenLeave_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync<ApiResult.EmptyResult>(ApiRoutes.Event.Single(eventId));

        Factory.ApiClient.SetAuth(joiner);
        var response = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Leave(eventId), new { });

        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  CONTACT VISIBILITY
    // ========================================

    [Fact]
    public async Task GivenApprovedParticipant_WhenGetDetail_ThenSeesPrivateContacts()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Contact Visibility",
                Description: "Testing private contacts",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: false,
                Contacts:
                [
                    new CreateEventContactDto(ContactType.Telegram, "publicuser", IsPublic: true),
                    new CreateEventContactDto(ContactType.Email, "private@test.com", IsPublic: false)
                ]));
        var eventId = Guid.Parse(response.Result.Id);

        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        var beforeJoin = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));
        beforeJoin.Success.ShouldBeTrue();
        beforeJoin.Result.Contacts.Length.ShouldBe(1);
        beforeJoin.Result.Contacts.ShouldContain(c => c.IsPublic && c.Value == "publicuser");
        beforeJoin.Result.Contacts.ShouldNotContain(c => !c.IsPublic);

        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        var afterJoin = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));

        afterJoin.Success.ShouldBeTrue();
        afterJoin.Result.Contacts.Length.ShouldBe(2);
        afterJoin.Result.Contacts.ShouldContain(c => c.IsPublic && c.Value == "publicuser");
        afterJoin.Result.Contacts.ShouldContain(c => !c.IsPublic && c.Value == "private@test.com");
    }

    [Fact]
    public async Task GivenPendingParticipant_WhenGetDetail_ThenSeesOnlyPublicContacts()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Pending Contact Visibility",
                Description: "Testing private contacts for pending user",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: true,
                Contacts:
                [
                    new CreateEventContactDto(ContactType.Telegram, "publicuser", IsPublic: true),
                    new CreateEventContactDto(ContactType.Email, "private@test.com", IsPublic: false)
                ]));
        var eventId = Guid.Parse(response.Result.Id);

        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        var eventDetail = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));

        eventDetail.Success.ShouldBeTrue();
        eventDetail.Result.Contacts.Length.ShouldBe(1);
        eventDetail.Result.Contacts.ShouldContain(c => c.IsPublic && c.Value == "publicuser");
        eventDetail.Result.Contacts.ShouldNotContain(c => !c.IsPublic);
    }

    [Fact]
    public async Task GivenNonParticipant_WhenGetDetail_ThenSeesOnlyPublicContacts()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Non-Participant Contact Visibility",
                Description: "Testing private contacts for non-participant",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: false,
                Contacts:
                [
                    new CreateEventContactDto(ContactType.Telegram, "publicuser", IsPublic: true),
                    new CreateEventContactDto(ContactType.Email, "private@test.com", IsPublic: false)
                ]));
        var eventId = Guid.Parse(response.Result.Id);

        var nonParticipant = await CreateUserAsync();
        Factory.ApiClient.SetAuth(nonParticipant);

        var eventDetail = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));

        eventDetail.Success.ShouldBeTrue();
        eventDetail.Result.Contacts.Length.ShouldBe(1);
        eventDetail.Result.Contacts.ShouldContain(c => c.IsPublic && c.Value == "publicuser");
        eventDetail.Result.Contacts.ShouldNotContain(c => !c.IsPublic);
    }

    [Fact]
    public async Task GivenPendingParticipant_WhenApprove_ThenApproved()
    {
        var (creatorAuth, eventId) = await CreateEventWithApprovalAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        Factory.ApiClient.SetAuth(creatorAuth);
        var response = await Factory.ApiClient.PatchAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.ApproveParticipant(eventId, Guid.Parse(joiner.UserId)), new { });

        response.Success.ShouldBeTrue();

        var eventDetail = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.Result.ApprovedParticipantIds.ShouldContain(Guid.Parse(joiner.UserId));
        eventDetail.Result.PendingParticipantIds.ShouldNotContain(Guid.Parse(joiner.UserId));
    }

    [Fact]
    public async Task GivenPendingParticipant_WhenReject_ThenRejected()
    {
        var (creatorAuth, eventId) = await CreateEventWithApprovalAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        Factory.ApiClient.SetAuth(creatorAuth);
        var response = await Factory.ApiClient.PatchAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.RejectParticipant(eventId, Guid.Parse(joiner.UserId)), new { });

        response.Success.ShouldBeTrue();

        var eventDetail = await Factory.ApiClient.GetAsync<ApiResult<EventDto>>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.Result.PendingParticipantIds.ShouldNotContain(Guid.Parse(joiner.UserId));
        eventDetail.Result.PendingApprovalsCount.ShouldBe(0);
    }

    [Fact]
    public async Task GivenNonCreator_WhenApprove_Then403()
    {
        var (_, eventId) = await CreateEventWithApprovalAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        var otherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(otherUser);
        var response = await Factory.ApiClient.PatchAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.ApproveParticipant(eventId, Guid.Parse(joiner.UserId)), new { });

        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  MY CREATED EVENTS
    // ========================================

    [Fact]
    public async Task GivenUserWithCreated_WhenMyCreated_ThenReturnsOwn()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<EventListItemDto>>>(
            ApiRoutes.Event.MyCreated + "?Offset=0&Limit=20");

        response.Success.ShouldBeTrue();
        response.Result.Data.Count.ShouldBe(1);
        response.Result.TotalCount.ShouldBe(1);
        response.Result.Data.First().Id.ShouldBe(eventId.ToString());
    }

    [Fact]
    public async Task GivenOldEvent_WhenMyCreatedNoDate_ThenExcluded()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var oldDate = DateTimeOffset.UtcNow.AddDays(-14);
        await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Old Meetup",
                Description: "Desc",
                StartDateTime: oldDate,
                VenueId: Guid.NewGuid()));

        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<EventListItemDto>>>(
            ApiRoutes.Event.MyCreated + "?Offset=0&Limit=20");

        response.Success.ShouldBeTrue();
        response.Result.Data.ShouldBeEmpty();
        response.Result.TotalCount.ShouldBe(0);
    }

    // ========================================
    //  MY PARTICIPATED EVENTS
    // ========================================

    [Fact]
    public async Task GivenUserWithJoined_WhenMyParticipated_ThenReturnsJoined()
    {
        var (_, eventId) = await CreateEventAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });

        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<EventListItemDto>>>(
            ApiRoutes.Event.MyParticipated + "?Offset=0&Limit=20");

        response.Success.ShouldBeTrue();
        response.Result.Data.Count.ShouldBe(1);
        response.Result.TotalCount.ShouldBe(1);
        response.Result.Data.First().Id.ShouldBe(eventId.ToString());
    }

    [Fact]
    public async Task GivenOwnEvent_WhenMyParticipated_ThenExcluded()
    {
        var (auth, _) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<EventListItemDto>>>(
            ApiRoutes.Event.MyParticipated + "?Offset=0&Limit=20");

        response.Success.ShouldBeTrue();
        response.Result.Data.ShouldBeEmpty();
        response.Result.TotalCount.ShouldBe(0);
    }

    // ========================================
    //  COVER UPLOAD URL
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenCoverUploadUrl_ThenReturnsUrls()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.Event.CoverUploadUrl(eventId),
            new GetPresignedUrlRequest(ContentType.ImageJpeg, 50000));

        response.Success.ShouldBeTrue();
        response.Result.UploadUrl.ShouldNotBeNullOrEmpty();
        response.Result.PublicDownloadUrl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenNonCreator_WhenCoverUploadUrl_Then403()
    {
        var (_, eventId) = await CreateEventAsync();
        var otherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(otherUser);

        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, ApiResult<GetPresignedUrlResponse>>(
            ApiRoutes.Event.CoverUploadUrl(eventId),
            new GetPresignedUrlRequest(ContentType.ImageJpeg, 50000));

        response.Success.ShouldBeFalse();
    }

    // ========================================
    //  DAILY CREATION LIMIT
    // ========================================

    [Fact]
    public async Task GivenUser_WhenCreatesMoreThan3Meetups_Then429()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        // Create exactly up to the limit — all should succeed
        for (var i = 0; i < EventParameters.MaxB2CMeetupsPerDay; i++)
        {
            var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
                ApiRoutes.Event.Root,
                new CreateEventRequest(
                    CreatorAvatarUrl: null,
                    CoverImageUrl: null,
                    Title: $"Meetup {i}",
                    Description: "Desc",
                    StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                    VenueId: Guid.NewGuid()));
            response.Success.ShouldBeTrue();
        }

        // One more should be rejected
        var fourthResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Too Many",
                Description: "Should fail",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid()));

        fourthResponse.Success.ShouldBeFalse();
        fourthResponse.Errors.ShouldNotBeNull();
        fourthResponse.Errors.Length.ShouldBe(1);
        fourthResponse.Errors[0].Error.ShouldBe(
            $"Daily creation limit of {EventParameters.MaxB2CMeetupsPerDay} reached");
        fourthResponse.Errors[0].Code.ShouldBe(nameof(ErrorCode.TooManyRequests));
    }

    [Fact]
    public async Task GivenUser_WhenCreates3MeetupsThenNextDay_ThenCanCreateAgain()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        // Create 3 meetups via API (today's CreatedAt)
        var eventIds = new List<Guid>();
        for (var i = 0; i < EventParameters.MaxB2CMeetupsPerDay; i++)
        {
            var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
                ApiRoutes.Event.Root,
                new CreateEventRequest(
                    CreatorAvatarUrl: null,
                    CoverImageUrl: null,
                    Title: $"Meetup {i}",
                    Description: "Desc",
                    StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                    VenueId: Guid.NewGuid()));
            response.Success.ShouldBeTrue();
            eventIds.Add(Guid.Parse(response.Result.Id));
        }

        // Move events to yesterday so daily count resets (raw SQL to bypass AuditableInterceptor)
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EventContext>();
            foreach (var id in eventIds)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE event.events SET created_at = {0} WHERE id = {1}",
                    yesterday, id);
            }
        }

        // Should now be able to create up to the limit again (daily count reset)
        for (var i = 0; i < EventParameters.MaxB2CMeetupsPerDay; i++)
        {
            var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
                ApiRoutes.Event.Root,
                new CreateEventRequest(
                    CreatorAvatarUrl: null,
                    CoverImageUrl: null,
                    Title: $"Today Meetup {i}",
                    Description: "Desc",
                    StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                    VenueId: Guid.NewGuid()));
            response.Success.ShouldBeTrue();
        }

        // Exceeding the limit again should be rejected
        var excess = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Too Many Again",
                Description: "Should fail",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid()));

        excess.Success.ShouldBeFalse();
        excess.Errors.ShouldNotBeNull();
        excess.Errors.Length.ShouldBe(1);
        excess.Errors[0].Code.ShouldBe(nameof(ErrorCode.TooManyRequests));
    }

    [Fact]
    public async Task GivenApprovalMeetup_WhenUserJoins_ThenCreatorReceivesJoinRequestNotification()
    {
        var creatorAuth = await Factory.AuthManager.CreateAndLoginUserAsync(
            "creator-join-req@example.com", "Password123!");
        Factory.ApiClient.SetAuth(creatorAuth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var joinerAuth = await Factory.AuthManager.CreateAndLoginUserAsync(
            "joiner-join-req@example.com", "Password123!");
        var joinerId = Guid.Parse(joinerAuth.UserId);
        Factory.ApiClient.SetAuth(joinerAuth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        Factory.ApiClient.SetAuth(creatorAuth);
        var tokenResponse = await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(
                ApiRoutes.Notifications.UpdateDeviceToken,
                new UpdateFcmTokenRequest("dev-creator-join-req", "token-creator-join-req"));
        tokenResponse.Success.ShouldBeTrue();

        Factory.ApiClient.SetAuth(creatorAuth);
        var createResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Approval Meetup Notif",
                Description: "Testing notifications",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: true));
        createResponse.Success.ShouldBeTrue();
        var eventId = Guid.Parse(createResponse.Result.Id);

        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(joinerAuth);
                var joinResponse = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
                    ApiRoutes.Event.Join(eventId), new { });
                joinResponse.Success.ShouldBeTrue();
            },
            expectedCount: 1,
            expectedType: FcmPushBuilder.PushType.UserRequestedApprove);

        Factory.ApiClient.SetAuth(creatorAuth);
        var notificationsResponse = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 20));
        notificationsResponse.Success.ShouldBeTrue();
        var notification = notificationsResponse.Result.Data
            .FirstOrDefault(n => n.Type == NotificationType.UserRequestedApprove && n.LinkedEntityId == eventId);
        notification.ShouldNotBeNull();
        notification.FromUserId.ShouldBe(joinerId);
        notification.Text.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenPendingParticipant_WhenApproved_ThenParticipantReceivesApprovedNotification()
    {
        var creatorAuth = await Factory.AuthManager.CreateAndLoginUserAsync(
            "creator-approve@example.com", "Password123!");
        var creatorId = Guid.Parse(creatorAuth.UserId);
        Factory.ApiClient.SetAuth(creatorAuth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        var joinerAuth = await Factory.AuthManager.CreateAndLoginUserAsync(
            "joiner-approve@example.com", "Password123!");
        var joinerId = Guid.Parse(joinerAuth.UserId);
        Factory.ApiClient.SetAuth(joinerAuth);
        await Factory.AuthManager.CreateDefaultProfileAndSubmitLocation();

        Factory.ApiClient.SetAuth(joinerAuth);
        var joinerTokenResponse = await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest, ApiResult.EmptyResult>(
                ApiRoutes.Notifications.UpdateDeviceToken,
                new UpdateFcmTokenRequest("dev-joiner-approve", "token-joiner-approve")
            );
        joinerTokenResponse.Success.ShouldBeTrue();

        Factory.ApiClient.SetAuth(creatorAuth);
        var createResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Approval Meetup Notif 2",
                Description: "Testing approval notification",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: true));
        createResponse.Success.ShouldBeTrue();
        var eventId = Guid.Parse(createResponse.Result.Id);

        Factory.ApiClient.SetAuth(joinerAuth);
        var joinResponse = await Factory.ApiClient.PostAsync<object, ApiResult.EmptyResult>(
            ApiRoutes.Event.Join(eventId), new { });
        joinResponse.Success.ShouldBeTrue();

        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(creatorAuth);
                var approveResponse = await Factory.ApiClient.PatchAsync<object, ApiResult.EmptyResult>(
                    ApiRoutes.Event.ApproveParticipant(eventId, joinerId), new { });
                approveResponse.Success.ShouldBeTrue();
            },
            expectedCount: 1,
            expectedType: FcmPushBuilder.PushType.ParticipationApproved);

        Factory.ApiClient.SetAuth(joinerAuth);
        var notificationsResponse = await Factory.ApiClient.GetAsync<ApiResult<PaginatedResponse<NotificationDto>>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 20));
        notificationsResponse.Success.ShouldBeTrue();
        var notification = notificationsResponse.Result.Data
            .FirstOrDefault(n => n.Type == NotificationType.UserParticipationApproved && n.LinkedEntityId == eventId);
        notification.ShouldNotBeNull();
        notification.FromUserId.ShouldBe(creatorId);
        notification.Text.ShouldNotBeNullOrEmpty();
    }

    // ========================================
    //  HELPERS
    // ========================================

    private async Task<AuthResponse> CreateUserAsync()
    {
        var email = $"b2c_event_{Guid.NewGuid():N}@example.com";
        return await Factory.AuthManager.CreateAndLoginUserAsync(
            email, "Password123!");
    }

    private async Task<(AuthResponse Auth, Guid EventId)> CreateEventAsync()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Test Meetup",
                Description: "Test Description",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: false));

        response.Success.ShouldBeTrue();
        return (auth, Guid.Parse(response.Result.Id));
    }

    private async Task<(AuthResponse Auth, Guid EventId)> CreateEventWithApprovalAsync()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Approval Meetup",
                Description: "Requires approval",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: true));

        response.Success.ShouldBeTrue();
        return (auth, Guid.Parse(response.Result.Id));
    }

    private async Task<Guid> SeedVenueAsync(double lat, double lng)
    {
        var b2BAuth = await Factory.AuthManager.CreateAndLoginUserAsync(
            $"b2b_venue_seed_{Guid.NewGuid():N}@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyResponse = await Factory.ApiClient.PostAsync<CreateCompanyRequest, ApiResult<CreateCompanyResponse>>(
            ApiRoutes.B2B.Company.Root,
            new CreateCompanyRequest(Name: "Seed Co", Email: null));
        companyResponse.Success.ShouldBeTrue();

        var venueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, ApiResult<CreateVenueResponse>>(
            ApiRoutes.B2B.Company.Venues(companyResponse.Result.Id),
            new CreateVenueRequest(
                Latitude: lat,
                Longitude: lng,
                Address: "Test Address",
                WebsiteUrl: null,
                GoogleMapsUrl: null,
                InstagramUrl: null,
                PhoneNumber: null,
                WorkingHours: null,
                CoverImageUrl: null,
                TagIds: []));
        venueResponse.Success.ShouldBeTrue();

        return venueResponse.Result.Id;
    }

    private async Task<Guid> CreateEventAtVenueAsync(AuthResponse auth, Guid venueId)
    {
        Factory.ApiClient.SetAuth(auth);
        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, ApiResult<EventDto>>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Map Event",
                Description: "Visible on map",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: venueId,
                MaxParticipants: null,
                RequiresApproval: false));

        response.Success.ShouldBeTrue();
        return Guid.Parse(response.Result.Id);
    }
}