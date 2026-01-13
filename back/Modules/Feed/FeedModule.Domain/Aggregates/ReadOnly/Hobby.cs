namespace FeedModule.Domain.Aggregates.ReadOnly;

public class Hobby
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;

    public ICollection<User> Users { get; init; } = new List<User>();

    internal Hobby()
    {
    }
}