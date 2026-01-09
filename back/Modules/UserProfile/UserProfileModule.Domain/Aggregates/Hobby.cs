namespace UserProfileModule.Domain.Aggregates;

public class Hobby
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;

    public List<UserProfile> UserProfiles { get; init; } = new();
}