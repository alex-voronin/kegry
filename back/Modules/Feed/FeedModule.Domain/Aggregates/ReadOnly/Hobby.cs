namespace FeedModule.Domain.Aggregates.ReadOnly;

public class Hobby
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;

    public ICollection<UserProfile> UserProfiles { get; init; } = new List<UserProfile>();

    internal Hobby()
    {
    }
}