using SharedCore.ValueObjects;
using UserProfiles.Domain.Aggregates;

namespace UserProfiles.API.Dto;

public class GetUserProfileResponse
{
    public required UserProfileDto UserProfile { get; init; }
}