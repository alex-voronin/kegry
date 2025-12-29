namespace UserProfileModule.API.Dto;

public class GetHobbyListResponse
{
    public required HobbyDto[] Hobbies { get; init; }
}