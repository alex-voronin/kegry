namespace UserProfiles.API.Dto;

public class GetHobbyListResponse
{
    public required HobbyDto[] Hobbies { get; init; }
}