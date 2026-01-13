namespace UserProfileModule.Domain.Aggregates;

public class Hobby
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;

    public List<User> Users { get; init; } = new();
}