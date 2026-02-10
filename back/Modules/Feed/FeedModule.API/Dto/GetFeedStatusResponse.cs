namespace FeedModule.API.Dto;

public record GetFeedStatusResponse(bool IsInMeeting, Guid? UserIdToRate, int AvailableInvitesCount);