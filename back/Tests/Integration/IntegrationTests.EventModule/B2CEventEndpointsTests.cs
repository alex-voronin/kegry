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
        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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

        response.ShouldNotBeNull();
        response.Title.ShouldBe("Fun Meetup");
        response.Description.ShouldBe("A great gathering");
        response.Type.ShouldBe(EventType.Meetup);
        response.IsCreator.ShouldBeTrue();
        response.CreatorId.ShouldBe(auth.UserId);
        response.ParticipationStatus.ShouldBe("Approved");
        response.ParticipantsCount.ShouldBe(1);
        response.RequiresApproval.ShouldBeFalse();
        response.IsVisibleOnMap.ShouldBeTrue();
        Guid.TryParse(response.Id, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithContactsAndHobbies_ThenCreated()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var hobbyId = "boxing".ToDeterministicGuid();
        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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

        response.Contacts.Length.ShouldBe(2);
        response.Contacts.ShouldContain(c => c.Type == ContactType.Telegram && c.Value == "testuser");
        response.HobbyIds.ShouldContain(hobbyId);
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithRequiresApproval_ThenCreated()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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

        response.RequiresApproval.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithMaxParticipants_ThenCreated()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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

        response.MaxParticipants.ShouldBe(5);
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithEmptyTitle_Then422()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var error = await Factory.ApiClient.PostProblemAsync(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "",
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid()));

        error.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.ValidationError));
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithTitleTooLong_Then422()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var error = await Factory.ApiClient.PostProblemAsync(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: new string('x', 201),
                Description: "Desc",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid()));

        error.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.ValidationError));
    }

    [Fact]
    public async Task GivenUser_WhenCreatesWithDescriptionTooLong_Then422()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var error = await Factory.ApiClient.PostProblemAsync(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Valid Title",
                Description: new string('x', 2001),
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid()));

        error.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.ValidationError));
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
        var response = await Factory.ApiClient.GetAsync<EventDto>(ApiRoutes.Event.Single(eventId));

        response.Id.ShouldBe(eventId.ToString());
        response.Title.ShouldBe("Test Meetup");
        response.IsCreator.ShouldBeTrue();
    }

    [Fact]
    public async Task GivenNonExistentEvent_WhenGet_Then404()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var error = await Factory.ApiClient.GetProblemAsync(
            ApiRoutes.Event.Single(Guid.NewGuid()));

        error.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenGet_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync(ApiRoutes.Event.Single(eventId));

        var error = await Factory.ApiClient.GetProblemAsync(ApiRoutes.Event.Single(eventId));

        error.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
    }

    [Fact]
    public async Task GivenExistingEvent_WhenGetByNonCreator_ThenReturnsDetail()
    {
        var (_, eventId) = await CreateEventAsync();
        var otherUser = await CreateUserAsync();

        Factory.ApiClient.SetAuth(otherUser);
        var response = await Factory.ApiClient.GetAsync<EventDto>(ApiRoutes.Event.Single(eventId));

        response.IsCreator.ShouldBeFalse();
        response.Id.ShouldBe(eventId.ToString());
    }

    // ========================================
    //  UPDATE EVENT
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenUpdatesTitleAndDescription_ThenUpdated()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PatchAsync<UpdateEventRequest, EventDto>(
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

        response.Title.ShouldBe("Updated Title");
        response.Description.ShouldBe("Updated Desc");
    }

    [Fact]
    public async Task GivenCreator_WhenUpdatesContacts_ThenReplaced()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PatchAsync<UpdateEventRequest, EventDto>(
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

        response.Contacts.Length.ShouldBe(1);
        response.Contacts[0].Value.ShouldBe("+381611111111");
    }

    [Fact]
    public async Task GivenCreator_WhenSetsIsVisibleOnMapToFalse_ThenUpdated()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PatchAsync<UpdateEventRequest, EventDto>(
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

        response.IsVisibleOnMap.ShouldBeFalse();
    }

    [Fact]
    public async Task GivenNonCreator_WhenUpdates_Then403()
    {
        var (_, eventId) = await CreateEventAsync();
        var otherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(otherUser);

        var error = await Factory.ApiClient.PatchProblemAsync(
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

        error.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotAllowed));
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenUpdates_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync(ApiRoutes.Event.Single(eventId));

        var error = await Factory.ApiClient.PatchProblemAsync(
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

        error.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
    }

    // ========================================
    //  DELETE EVENT
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenDeletes_ThenSoftDeleted()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        await Factory.ApiClient.DeleteAsync(
            ApiRoutes.Event.Single(eventId));

        var getError = await Factory.ApiClient.GetProblemAsync(
            ApiRoutes.Event.Single(eventId));
        getError.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        getError.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
    }

    [Fact]
    public async Task GivenNonCreator_WhenDeletes_Then403()
    {
        var (_, eventId) = await CreateEventAsync();
        var otherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(otherUser);

        var error = await Factory.ApiClient.DeleteProblemAsync(
            ApiRoutes.Event.Single(eventId));
        error.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotAllowed));
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenDeletesAgain_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync(ApiRoutes.Event.Single(eventId));

        var error = await Factory.ApiClient.DeleteProblemAsync(
            ApiRoutes.Event.Single(eventId));
        error.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
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

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, VenueEventsDto[]>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 44.78,
                MaxLat: 44.80,
                MinLng: 20.44,
                MaxLng: 20.46));

        response.Length.ShouldBe(2);
        response.ShouldContain(v => v.VenueId == venueAId.ToString());
        response.ShouldContain(v => v.VenueId == venueBId.ToString());
        response.Single(v => v.VenueId == venueAId.ToString()).Events.Length.ShouldBe(1);
    }

    [Fact]
    public async Task GivenEventsOutsideBounds_WhenGetMap_ThenEmpty()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        await CreateEventAtVenueAsync(auth, venueId);

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, VenueEventsDto[]>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 45.0,
                MaxLat: 46.0,
                MinLng: 21.0,
                MaxLng: 22.0));

        response.ShouldBeEmpty();
    }

    [Fact]
    public async Task GivenEmptyEventTypes_WhenGetMap_ThenEmpty()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        await CreateEventAtVenueAsync(auth, venueId);

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, VenueEventsDto[]>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 44.78,
                MaxLat: 44.80,
                MinLng: 20.44,
                MaxLng: 20.46,
                EventTypes: []));

        response.ShouldBeEmpty();
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
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Feed.BlockUser(userB.UserId), new { });

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, VenueEventsDto[]>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 44.78,
                MaxLat: 44.80,
                MinLng: 20.44,
                MaxLng: 20.46));

        response.ShouldBeEmpty();
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

        var response = await Factory.ApiClient.GetAsync<PaginatedResponse<EventListItemDto>>(
            ApiRoutes.Event.VenueEvents(venueId) + "?Offset=0&Limit=20");

        response.Data.Count.ShouldBe(1);
        response.TotalCount.ShouldBe(1);
        response.Data.First().Id.ShouldBe(eventId.ToString());
    }

    [Fact]
    public async Task GivenVenueWithInvisibleEvent_WhenGetVenueEvents_ThenExcluded()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        var eventId = await CreateEventAtVenueAsync(auth, venueId);

        await Factory.ApiClient.PatchAsync<UpdateEventRequest, EventDto>(
            ApiRoutes.Event.Single(eventId),
            new UpdateEventRequest(
                Title: null, Description: null, StartDateTime: null,
                IsVisibleOnMap: false, MaxParticipants: null, RequiresApproval: null,
                CoverImageUrl: null, Contacts: null, HobbyIds: null));

        var response = await Factory.ApiClient.GetAsync<PaginatedResponse<EventListItemDto>>(
            ApiRoutes.Event.VenueEvents(venueId) + "?Offset=0&Limit=20");

        response.Data.ShouldBeEmpty();
        response.TotalCount.ShouldBe(0);
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

        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });


        Factory.ApiClient.SetAuth(creatorAuth);
        var eventDetail = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.ParticipantsCount.ShouldBe(2);
        eventDetail.ApprovedParticipantIds.ShouldContain(Guid.Parse(joiner.UserId));
    }

    [Fact]
    public async Task GivenApprovalEvent_WhenJoin_ThenPending()
    {
        var (creatorAuth, eventId) = await CreateEventWithApprovalAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });


        Factory.ApiClient.SetAuth(creatorAuth);
        var eventDetail = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.PendingApprovalsCount.ShouldBe(1);
        eventDetail.PendingParticipantIds.ShouldContain(Guid.Parse(joiner.UserId));
    }

    [Fact]
    public async Task GivenAlreadyJoined_WhenJoinAgain_Then409()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var error = await Factory.ApiClient.PostProblemAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        error.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.Conflict);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.AlreadyExists));
    }

    [Fact]
    public async Task GivenPublicEventAtMaxCapacity_WhenJoin_Then409()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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

        var eventId = Guid.Parse(createResponse.Id);
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        var error = await Factory.ApiClient.PostProblemAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        error.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.Conflict);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.TooManyEntities));
    }

    [Fact]
    public async Task GivenApprovalEventWithPendingAtMax_WhenJoin_Then422()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var createResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Full Approval Meetup",
                Description: "1 open spot, requires approval",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(7),
                VenueId: Guid.NewGuid(),
                MaxParticipants: 2,
                RequiresApproval: true));

        var eventId = Guid.Parse(createResponse.Id);
        var filler = await CreateUserAsync();
        Factory.ApiClient.SetAuth(filler);
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        var anotherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(anotherUser);
        var error = await Factory.ApiClient.PostProblemAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        error.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.UnprocessableEntity);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.ValidationError));
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenJoin_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync(ApiRoutes.Event.Single(eventId));

        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        var error = await Factory.ApiClient.PostProblemAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        error.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
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
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Leave(eventId), new { });

        Factory.ApiClient.SetAuth(creatorAuth);
        var eventDetail = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.ParticipantsCount.ShouldBe(1);
    }

    [Fact]
    public async Task GivenNonParticipant_WhenLeave_Then409()
    {
        var (_, eventId) = await CreateEventAsync();
        var nonParticipant = await CreateUserAsync();
        Factory.ApiClient.SetAuth(nonParticipant);

        var error = await Factory.ApiClient.PostProblemAsync<object>(
            ApiRoutes.Event.Leave(eventId), new { });

        error.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.Conflict);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.DoesNotExist));
    }

    [Fact]
    public async Task GivenCreator_WhenLeave_Then403()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var error = await Factory.ApiClient.PostProblemAsync<object>(
            ApiRoutes.Event.Leave(eventId), new { });

        error.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.Forbidden);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotAllowed));
    }

    [Fact]
    public async Task GivenDeletedEvent_WhenLeave_Then404()
    {
        var (auth, eventId) = await CreateEventAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        Factory.ApiClient.SetAuth(auth);
        await Factory.ApiClient.DeleteAsync(ApiRoutes.Event.Single(eventId));

        Factory.ApiClient.SetAuth(joiner);
        var error = await Factory.ApiClient.PostProblemAsync<object>(
            ApiRoutes.Event.Leave(eventId), new { });

        error.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
    }

    // ========================================
    //  CONTACT VISIBILITY
    // ========================================

    [Fact]
    public async Task GivenApprovedParticipant_WhenGetDetail_ThenSeesPrivateContacts()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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
        var eventId = Guid.Parse(response.Id);

        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);

        var beforeJoin = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));
        beforeJoin.Contacts.Length.ShouldBe(1);
        beforeJoin.Contacts.ShouldContain(c => c.IsPublic && c.Value == "publicuser");
        beforeJoin.Contacts.ShouldNotContain(c => !c.IsPublic);

        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        var afterJoin = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));

        afterJoin.Contacts.Length.ShouldBe(2);
        afterJoin.Contacts.ShouldContain(c => c.IsPublic && c.Value == "publicuser");
        afterJoin.Contacts.ShouldContain(c => !c.IsPublic && c.Value == "private@test.com");
    }

    [Fact]
    public async Task GivenPendingParticipant_WhenGetDetail_ThenSeesOnlyPublicContacts()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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
        var eventId = Guid.Parse(response.Id);

        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        var eventDetail = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));

        eventDetail.Contacts.Length.ShouldBe(1);
        eventDetail.Contacts.ShouldContain(c => c.IsPublic && c.Value == "publicuser");
        eventDetail.Contacts.ShouldNotContain(c => !c.IsPublic);
    }

    [Fact]
    public async Task GivenNonParticipant_WhenGetDetail_ThenSeesOnlyPublicContacts()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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
        var eventId = Guid.Parse(response.Id);

        var nonParticipant = await CreateUserAsync();
        Factory.ApiClient.SetAuth(nonParticipant);

        var eventDetail = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));

        eventDetail.Contacts.Length.ShouldBe(1);
        eventDetail.Contacts.ShouldContain(c => c.IsPublic && c.Value == "publicuser");
        eventDetail.Contacts.ShouldNotContain(c => !c.IsPublic);
    }

    [Fact]
    public async Task GivenPendingParticipant_WhenApprove_ThenApproved()
    {
        var (creatorAuth, eventId) = await CreateEventWithApprovalAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        Factory.ApiClient.SetAuth(creatorAuth);
        var response = await Factory.ApiClient.PatchAsync<object>(
            ApiRoutes.Event.ApproveParticipant(eventId, Guid.Parse(joiner.UserId)), new { });


        var eventDetail = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.ApprovedParticipantIds.ShouldContain(Guid.Parse(joiner.UserId));
        eventDetail.PendingParticipantIds.ShouldNotContain(Guid.Parse(joiner.UserId));
    }

    [Fact]
    public async Task GivenPendingParticipant_WhenReject_ThenRejected()
    {
        var (creatorAuth, eventId) = await CreateEventWithApprovalAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        Factory.ApiClient.SetAuth(creatorAuth);
        var response = await Factory.ApiClient.PatchAsync<object>(
            ApiRoutes.Event.RejectParticipant(eventId, Guid.Parse(joiner.UserId)), new { });


        var eventDetail = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));
        eventDetail.PendingParticipantIds.ShouldNotContain(Guid.Parse(joiner.UserId));
        eventDetail.PendingApprovalsCount.ShouldBe(0);
    }

    [Fact]
    public async Task GivenNonCreator_WhenApprove_Then403()
    {
        var (_, eventId) = await CreateEventWithApprovalAsync();
        var joiner = await CreateUserAsync();
        Factory.ApiClient.SetAuth(joiner);
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        var otherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(otherUser);
        var error = await Factory.ApiClient.PatchProblemAsync<object>(
            ApiRoutes.Event.ApproveParticipant(eventId, Guid.Parse(joiner.UserId)), new { });

        error.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.Forbidden);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotAllowed));
    }

    // ========================================
    //  MY CREATED EVENTS
    // ========================================

    [Fact]
    public async Task GivenUserWithCreated_WhenMyCreated_ThenReturnsOwn()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.GetAsync<PaginatedResponse<EventListItemDto>>(
            ApiRoutes.Event.MyCreated + "?Offset=0&Limit=20");

        response.Data.Count.ShouldBe(1);
        response.TotalCount.ShouldBe(1);
        response.Data.First().Id.ShouldBe(eventId.ToString());
    }

    [Fact]
    public async Task GivenOldEvent_WhenMyCreatedNoDate_ThenExcluded()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var oldDate = DateTimeOffset.UtcNow.AddDays(-14);
        await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Old Meetup",
                Description: "Desc",
                StartDateTime: oldDate,
                VenueId: Guid.NewGuid()));

        var response = await Factory.ApiClient.GetAsync<PaginatedResponse<EventListItemDto>>(
            ApiRoutes.Event.MyCreated + "?Offset=0&Limit=20");

        response.Data.ShouldBeEmpty();
        response.TotalCount.ShouldBe(0);
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
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        var response = await Factory.ApiClient.GetAsync<PaginatedResponse<EventListItemDto>>(
            ApiRoutes.Event.MyParticipated + "?Offset=0&Limit=20");

        response.Data.Count.ShouldBe(1);
        response.TotalCount.ShouldBe(1);
        response.Data.First().Id.ShouldBe(eventId.ToString());
    }

    [Fact]
    public async Task GivenOwnEvent_WhenMyParticipated_ThenExcluded()
    {
        var (auth, _) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.GetAsync<PaginatedResponse<EventListItemDto>>(
            ApiRoutes.Event.MyParticipated + "?Offset=0&Limit=20");

        response.Data.ShouldBeEmpty();
        response.TotalCount.ShouldBe(0);
    }

    // ========================================
    //  COVER UPLOAD URL
    // ========================================

    [Fact]
    public async Task GivenCreator_WhenCoverUploadUrl_ThenReturnsUrls()
    {
        var (auth, eventId) = await CreateEventAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<GetPresignedUrlRequest, GetPresignedUrlResponse>(
            ApiRoutes.Event.CoverUploadUrl(eventId),
            new GetPresignedUrlRequest(ContentType.ImageJpeg, 50000));

        response.UploadUrl.ShouldNotBeNullOrEmpty();
        response.PublicDownloadUrl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GivenNonCreator_WhenCoverUploadUrl_Then403()
    {
        var (_, eventId) = await CreateEventAsync();
        var otherUser = await CreateUserAsync();
        Factory.ApiClient.SetAuth(otherUser);

        var error = await Factory.ApiClient.PostProblemAsync(
            ApiRoutes.Event.CoverUploadUrl(eventId),
            new GetPresignedUrlRequest(ContentType.ImageJpeg, 50000));

        error.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.Forbidden);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotAllowed));
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
            var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
                ApiRoutes.Event.Root,
                new CreateEventRequest(
                    CreatorAvatarUrl: null,
                    CoverImageUrl: null,
                    Title: $"Meetup {i}",
                    Description: "Desc",
                    StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                    VenueId: Guid.NewGuid()));
        }

        // One more should be rejected
        var error = await Factory.ApiClient.PostProblemAsync(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Too Many",
                Description: "Should fail",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid()));

        error.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        error.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.TooManyRequests);
        error.ProblemDetails.Detail.ShouldBe(
            $"Daily creation limit of {EventParameters.MaxB2CMeetupsPerDay} reached");
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.TooManyRequests));
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
            var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
                ApiRoutes.Event.Root,
                new CreateEventRequest(
                    CreatorAvatarUrl: null,
                    CoverImageUrl: null,
                    Title: $"Meetup {i}",
                    Description: "Desc",
                    StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                    VenueId: Guid.NewGuid()));
            eventIds.Add(Guid.Parse(response.Id));
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
            var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
                ApiRoutes.Event.Root,
                new CreateEventRequest(
                    CreatorAvatarUrl: null,
                    CoverImageUrl: null,
                    Title: $"Today Meetup {i}",
                    Description: "Desc",
                    StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                    VenueId: Guid.NewGuid()));
        }

        // Exceeding the limit again should be rejected
        var excessError = await Factory.ApiClient.PostProblemAsync(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Too Many Again",
                Description: "Should fail",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid()));

        excessError.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        excessError.ProblemDetails.Status.ShouldBe((int)HttpStatusCode.TooManyRequests);
        excessError.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.TooManyRequests));
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
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest>(
                ApiRoutes.Notifications.UpdateDeviceToken,
                new UpdateFcmTokenRequest("dev-creator-join-req", "token-creator-join-req"));

        Factory.ApiClient.SetAuth(creatorAuth);
        var createResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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
        var eventId = Guid.Parse(createResponse.Id);

        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(joinerAuth);
                await Factory.ApiClient.PostAsync<object>(
                    ApiRoutes.Event.Join(eventId), new { });
            },
            expectedCount: 1,
            expectedType: FcmPushBuilder.PushType.UserRequestedApprove);

        Factory.ApiClient.SetAuth(creatorAuth);
        var notificationsResponse = await Factory.ApiClient.GetAsync<PaginatedResponse<NotificationDto>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 20));
        var notification = notificationsResponse.Data
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
        await Factory.ApiClient
            .PostAsync<UpdateFcmTokenRequest>(
                ApiRoutes.Notifications.UpdateDeviceToken,
                new UpdateFcmTokenRequest("dev-joiner-approve", "token-joiner-approve")
            );

        Factory.ApiClient.SetAuth(creatorAuth);
        var createResponse = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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
        var eventId = Guid.Parse(createResponse.Id);

        Factory.ApiClient.SetAuth(joinerAuth);
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Event.Join(eventId), new { });

        await Factory.NotificationManager.AwaitPushNotifications(
            codeToRun: async () =>
            {
                Factory.ApiClient.SetAuth(creatorAuth);
                await Factory.ApiClient.PatchAsync<object>(
                    ApiRoutes.Event.ApproveParticipant(eventId, joinerId), new { });
            },
            expectedCount: 1,
            expectedType: FcmPushBuilder.PushType.ParticipationApproved);

        Factory.ApiClient.SetAuth(joinerAuth);
        var notificationsResponse = await Factory.ApiClient.GetAsync<PaginatedResponse<NotificationDto>>(
            ApiRoutes.Notifications.GetNotificationsPaginated(0, 20));
        var notification = notificationsResponse.Data
            .FirstOrDefault(n => n.Type == NotificationType.UserParticipationApproved && n.LinkedEntityId == eventId);
        notification.ShouldNotBeNull();
        notification.FromUserId.ShouldBe(creatorId);
        notification.Text.ShouldNotBeNullOrEmpty();
    }

    // ========================================
    //  ANONYMOUS ACCESS (PUBLIC ENDPOINTS)
    // ========================================

    [Fact]
    public async Task GivenEventsInBounds_WhenAnonymousGetsMap_ThenReturnsWithoutParticipationStatus()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        await CreateEventAtVenueAsync(auth, venueId);

        Factory.HttpClient.DefaultRequestHeaders.Authorization = null;

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, VenueEventsDto[]>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 44.78,
                MaxLat: 44.80,
                MinLng: 20.44,
                MaxLng: 20.46));

        response.Length.ShouldBe(1);
        response[0].Events.Length.ShouldBe(1);
        response[0].Events[0].ParticipationStatus.ShouldBeNull();
    }

    [Fact]
    public async Task GivenBlockedCreator_WhenAnonymousGetsMap_ThenBlockedEventsStillVisible()
    {
        var userA = await CreateUserAsync();
        var userB = await CreateUserAsync();

        Factory.ApiClient.SetAuth(userA);
        var venueId = await SeedVenueAsync(44.7866, 20.4489);

        Factory.ApiClient.SetAuth(userB);
        await CreateEventAtVenueAsync(userB, venueId);

        Factory.ApiClient.SetAuth(userA);
        await Factory.ApiClient.PostAsync<object>(
            ApiRoutes.Feed.BlockUser(userB.UserId), new { });

        Factory.HttpClient.DefaultRequestHeaders.Authorization = null;

        var response = await Factory.ApiClient.PostAsync<GetEventsMapRequest, VenueEventsDto[]>(
            ApiRoutes.Event.Map,
            new GetEventsMapRequest(
                MinLat: 44.78,
                MaxLat: 44.80,
                MinLng: 20.44,
                MaxLng: 20.46));

        response.Length.ShouldBe(1);
    }

    [Fact]
    public async Task GivenExistingEvent_WhenAnonymousGetsDetail_ThenReturnsWithoutAuth()
    {
        var (auth, eventId) = await CreateEventWithContactsAsync();

        Factory.HttpClient.DefaultRequestHeaders.Authorization = null;

        var response = await Factory.ApiClient.GetAsync<EventDto>(
            ApiRoutes.Event.Single(eventId));

        response.Id.ShouldBe(eventId.ToString());
        response.IsCreator.ShouldBeFalse();
        response.ParticipationStatus.ShouldBeNull();
        response.Contacts.ShouldNotBeEmpty();
        response.Contacts.ShouldAllBe(c => c.IsPublic);
    }

    [Fact]
    public async Task GivenNonExistentEvent_WhenAnonymousGetsDetail_Then404()
    {
        Factory.HttpClient.DefaultRequestHeaders.Authorization = null;

        var error = await Factory.ApiClient.GetProblemAsync(
            ApiRoutes.Event.Single(Guid.NewGuid()));

        error.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        error.ProblemDetails.GetCode().ShouldBe(nameof(ErrorCode.NotFound));
    }

    [Fact]
    public async Task GivenVenueWithEvents_WhenAnonymousGetsVenueEvents_ThenReturnsWithoutParticipationStatus()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var venueId = await SeedVenueAsync(44.7866, 20.4489);
        await CreateEventAtVenueAsync(auth, venueId);

        Factory.HttpClient.DefaultRequestHeaders.Authorization = null;

        var response = await Factory.ApiClient.GetAsync<PaginatedResponse<EventListItemDto>>(
            ApiRoutes.Event.VenueEvents(venueId) + "?Offset=0&Limit=20");

        response.Data.Count.ShouldBe(1);
        response.TotalCount.ShouldBe(1);
        response.Data.First().ParticipationStatus.ShouldBeNull();
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

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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

        return (auth, Guid.Parse(response.Id));
    }

    private async Task<(AuthResponse Auth, Guid EventId)> CreateEventWithApprovalAsync()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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

        return (auth, Guid.Parse(response.Id));
    }

    private async Task<Guid> SeedVenueAsync(double lat, double lng)
    {
        var b2BAuth = await Factory.AuthManager.CreateAndLoginUserAsync(
            $"b2b_venue_seed_{Guid.NewGuid():N}@example.com", "Password123!", UserType.B2B);
        Factory.ApiClient.SetAuth(b2BAuth);

        var companyResponse = await Factory.ApiClient.PostAsync<CreateCompanyRequest, CreateCompanyResponse>(
            ApiRoutes.B2B.Company.Root,
            new CreateCompanyRequest(Name: "Seed Co", Email: null));

        var venueResponse = await Factory.ApiClient.PostAsync<CreateVenueRequest, CreateVenueResponse>(
            ApiRoutes.B2B.Company.Venues(companyResponse.Id),
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

        return venueResponse.Id;
    }

    private async Task<(AuthResponse Auth, Guid EventId)> CreateEventWithContactsAsync()
    {
        var auth = await CreateUserAsync();
        Factory.ApiClient.SetAuth(auth);

        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
            ApiRoutes.Event.Root,
            new CreateEventRequest(
                CreatorAvatarUrl: null,
                CoverImageUrl: null,
                Title: "Contacts Meetup",
                Description: "Has contacts",
                StartDateTime: DateTimeOffset.UtcNow.AddDays(14),
                VenueId: Guid.NewGuid(),
                MaxParticipants: null,
                RequiresApproval: false,
                Contacts:
                [
                    new CreateEventContactDto(ContactType.Telegram, "publicuser", IsPublic: true),
                    new CreateEventContactDto(ContactType.Email, "private@test.com", IsPublic: false)
                ]));

        return (auth, Guid.Parse(response.Id));
    }

    private async Task<Guid> CreateEventAtVenueAsync(AuthResponse auth, Guid venueId)
    {
        Factory.ApiClient.SetAuth(auth);
        var response = await Factory.ApiClient.PostAsync<CreateEventRequest, EventDto>(
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

        return Guid.Parse(response.Id);
    }
}